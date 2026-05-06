using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.FailAnalysisProcessing;

public sealed class FailAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> _integrationEventMapper;
    private readonly ILogger<FailAnalysisProcessingHandler> _logger;
    private readonly ActivitySource _activitySource;

    public FailAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> integrationEventMapper,
        ILogger<FailAnalysisProcessingHandler> logger,
        ActivitySource activitySource)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<Result<FailAnalysisProcessingResult>> HandleAsync(
        FailAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ProcessingService fail analysis processing",
            ActivityKind.Internal);

        activity?.SetTag("analysis.request.id", command.AnalysisRequestId);
        activity?.SetTag("analysis.failure.reason", command.Reason);
        activity?.SetTag("analysis.failure.has_details", !string.IsNullOrWhiteSpace(command.Details));
        activity?.SetTag("analysis.failure.details.length", command.Details?.Length ?? 0);

        _logger.LogInformation(
            "Handling {CommandType} for AnalysisRequestId={AnalysisRequestId}",
            nameof(FailAnalysisProcessingCommand),
            command.AnalysisRequestId);

        try
        {
            var process = await _repository.GetByAnalysisRequestIdAsync(
                AnalysisRequestId.Create(command.AnalysisRequestId),
                cancellationToken);

            if (process is null)
            {
                const string errorMessage = "Analysis process was not found.";

                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("error.type", "processing.not_found");
                activity?.SetTag("error.message", errorMessage);

                _logger.LogWarning(
                    "No analysis process found for AnalysisRequestId={AnalysisRequestId}. Cannot mark processing as failed.",
                    command.AnalysisRequestId);

                return Result.Failure<FailAnalysisProcessingResult>(
                    Error.NotFound(
                        "processing.not_found",
                        $"Analysis process for request '{command.AnalysisRequestId}' was not found."));
            }

            activity?.SetTag("processing.process.id", process.Id);
            activity?.SetTag("processing.previous_status", process.Status.ToString());

            var failedAtUtc = _dateTimeProvider.UtcNow;
            process.MarkAsFailed(command.Reason, command.Details, failedAtUtc);

            activity?.SetTag("processing.current_status", process.Status.ToString());
            activity?.SetTag("processing.failed_at_utc", failedAtUtc.ToString("O"));

            _repository.Update(process);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var domainEvent = process
                .DequeueDomainEvents()
                .OfType<AnalysisProcessingFailedDomainEvent>()
                .Single();

            activity?.SetTag("messaging.event.type", domainEvent.GetType().Name);
            activity?.SetTag("messaging.event.id", domainEvent.EventId);

            var integrationEvent = _integrationEventMapper.Map(domainEvent);

            activity?.SetTag("messaging.integration_event.type", integrationEvent.EventType);
            activity?.SetTag("messaging.integration_event.id", integrationEvent.EventId);
            activity?.SetTag("correlation.id", integrationEvent.CorrelationId);

            if (integrationEvent.CausationId is not null)
            {
                activity?.SetTag("causation.id", integrationEvent.CausationId.Value);
            }

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Marked analysis processing as failed. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}, Reason={Reason}",
                process.Id,
                process.AnalysisRequestId,
                command.Reason);

            return Result.Success(new FailAnalysisProcessingResult(
                process.Id,
                process.AnalysisRequestId.Value,
                process.Status,
                failedAtUtc,
                process.FailureReason!,
                process.FailureDetails));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "Failed to mark analysis processing as failed for AnalysisRequestId={AnalysisRequestId}.",
                command.AnalysisRequestId);

            var errorCode = "processing.error";

            if (ex is ValidationException)
                errorCode = "processing.validation_error";
            else if (ex is DomainException)
                errorCode = "processing.domain_error";
            else if (ex is ArgumentException)
                errorCode = "processing.invalid_argument";

            return Result.Failure<FailAnalysisProcessingResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }
}
