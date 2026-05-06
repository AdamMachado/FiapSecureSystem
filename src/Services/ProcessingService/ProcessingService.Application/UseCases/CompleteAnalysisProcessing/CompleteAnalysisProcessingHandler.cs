using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.CompleteAnalysisProcessing;

public sealed class CompleteAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent> _integrationEventMapper;
    private readonly ILogger<CompleteAnalysisProcessingHandler> _logger;
    private readonly ActivitySource _activitySource;

    public CompleteAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent> integrationEventMapper,
        ILogger<CompleteAnalysisProcessingHandler> logger,
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

    public async Task<Result<CompleteAnalysisProcessingResult>> HandleAsync(
        CompleteAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ProcessingService complete analysis processing",
            ActivityKind.Internal);

        activity?.SetTag("analysis.request.id", command.AnalysisRequestId);
        activity?.SetTag("analysis.components.count", command.Components.Count);
        activity?.SetTag("analysis.risks.count", command.Risks.Count);
        activity?.SetTag("analysis.recommendations.count", command.Recommendations.Count);
        activity?.SetTag("analysis.requires_manual_review", command.RequiresManualReview);
        activity?.SetTag("analysis.warnings.count", command.Warnings.Count);
        activity?.SetTag("analysis.extracted_text.length", command.ExtractedText?.Length ?? 0);

        _logger.LogInformation(
            "Handling {CommandType} for AnalysisRequestId={AnalysisRequestId}",
            nameof(CompleteAnalysisProcessingCommand),
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
                    "No analysis process found for AnalysisRequestId={AnalysisRequestId}. Cannot complete processing.",
                    command.AnalysisRequestId);

                return Result.Failure<CompleteAnalysisProcessingResult>(
                    Error.NotFound(
                        "processing.not_found",
                        $"Analysis process for request '{command.AnalysisRequestId}' was not found."));
            }

            activity?.SetTag("processing.process.id", process.Id);
            activity?.SetTag("processing.previous_status", process.Status.ToString());

            var summary = ProcessingResultSummary.Create(
                command.Overview,
                command.Components.Count,
                command.Risks.Count,
                command.Recommendations.Count,
                command.RequiresManualReview,
                command.Warnings);

            var completedAtUtc = _dateTimeProvider.UtcNow;

            process.MarkAsCompleted(
                ExtractedText.Create(command.ExtractedText),
                command.Components,
                command.Risks,
                command.Recommendations,
                summary,
                completedAtUtc);

            activity?.SetTag("processing.current_status", process.Status.ToString());
            activity?.SetTag("processing.completed_at_utc", completedAtUtc.ToString("O"));

            _repository.Update(process);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var domainEvent = process
                .DequeueDomainEvents()
                .OfType<AnalysisProcessingCompletedDomainEvent>()
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
                "Completed analysis processing for AnalysisRequestId={AnalysisRequestId}. Total components: {ComponentCount}, Risks: {RiskCount}, Recommendations: {RecommendationCount}",
                command.AnalysisRequestId,
                process.Components.Count,
                process.Risks.Count,
                process.Recommendations.Count);

            return Result.Success(new CompleteAnalysisProcessingResult(
                process.Id,
                process.AnalysisRequestId.Value,
                process.Status,
                completedAtUtc,
                process.Components.Count,
                process.Risks.Count,
                process.Recommendations.Count));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "Failed to complete analysis processing for AnalysisRequestId={AnalysisRequestId}.",
                command.AnalysisRequestId);

            var errorCode = "processing.error";

            if (ex is ValidationException)
                errorCode = "processing.validation_error";
            else if (ex is DomainException)
                errorCode = "processing.domain_error";
            else if (ex is ArgumentException)
                errorCode = "processing.invalid_argument";

            return Result.Failure<CompleteAnalysisProcessingResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }
}
