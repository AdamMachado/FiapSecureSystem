using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Integration.Published;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.StartAnalysisProcessing;

public sealed class StartAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent> _startedIntegrationEventMapper;
    private readonly AnalysisExecutionRequestedIntegrationEventFactory _executionRequestedIntegrationEventFactory;

    private readonly ILogger<StartAnalysisProcessingHandler> _logger;
    private readonly ActivitySource _activitySource;

    public StartAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent> startedIntegrationEventMapper,
        AnalysisExecutionRequestedIntegrationEventFactory executionRequestedIntegrationEventFactory,
        ILogger<StartAnalysisProcessingHandler> logger,
        ActivitySource activitySource)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _eventPublisher = eventPublisher;
        _startedIntegrationEventMapper = startedIntegrationEventMapper;
        _executionRequestedIntegrationEventFactory = executionRequestedIntegrationEventFactory;

        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<Result<StartAnalysisProcessingResult>> HandleAsync(
        StartAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ProcessingService start analysis processing",
            ActivityKind.Internal);

        activity?.SetTag("analysis.request.id", command.AnalysisRequestId);
        activity?.SetTag("analysis.requested_by_user.id", command.RequestedByUserId);
        activity?.SetTag("analysis.file.name", command.FileName);
        activity?.SetTag("analysis.file.content_type", command.ContentType);
        activity?.SetTag("storage.bucket", command.StorageBucket);
        activity?.SetTag("storage.object.key", command.StorageObjectKey);

        _logger.LogInformation(
            "Handling StartAnalysisProcessingCommand. AnalysisRequestId={AnalysisRequestId}, RequestedByUserId={RequestedByUserId}",
            command.AnalysisRequestId,
            command.RequestedByUserId);

        try
        {
            var analysisRequestId = AnalysisRequestId.Create(command.AnalysisRequestId);
            var process = await _repository.GetByAnalysisRequestIdAsync(
                analysisRequestId,
                cancellationToken);

            var nowUtc = _dateTimeProvider.UtcNow;

            activity?.SetTag("processing.process.already_exists", process is not null);

            AnalysisProcessingStartedDomainEvent? startedDomainEvent = null;

            if (process is null)
            {
                var diagramType = ResolveDiagramType(command.ContentType);

                activity?.SetTag("analysis.diagram_type", diagramType.ToString());
                activity?.SetTag("processing.started_at_utc", nowUtc.ToString("O"));

                process = AnalysisProcess.Create(
                    Guid.NewGuid(),
                    analysisRequestId,
                    command.RequestedByUserId,
                    SourceFileLocation.Create(command.StorageBucket, command.StorageObjectKey),
                    diagramType,
                    nowUtc);

                activity?.SetTag("processing.process.id", process.Id);
                activity?.SetTag("processing.previous_status", process.Status.ToString());

                process.MarkAsStarted(nowUtc);

                activity?.SetTag("processing.current_status", process.Status.ToString());

                await _repository.AddAsync(process, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                startedDomainEvent = process
                    .DequeueDomainEvents()
                    .OfType<AnalysisProcessingStartedDomainEvent>()
                    .Single();

                _logger.LogInformation(
                    "Started analysis process admission. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}",
                    process.Id,
                    process.AnalysisRequestId,
                    process.DiagramType);
            }
            else
            {
                activity?.SetTag("processing.process.id", process.Id);
                activity?.SetTag("processing.previous_status", process.Status.ToString());
                activity?.SetTag("processing.current_status", process.Status.ToString());
                activity?.SetTag("analysis.diagram_type", process.DiagramType.ToString());

                if (process.Status == ProcessingStatus.Pending)
                {
                    process.MarkAsStarted(nowUtc);
                    _repository.Update(process);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    startedDomainEvent = process
                        .DequeueDomainEvents()
                        .OfType<AnalysisProcessingStartedDomainEvent>()
                        .Single();

                    activity?.SetTag("processing.started_at_utc", nowUtc.ToString("O"));
                    activity?.SetTag("processing.current_status", process.Status.ToString());
                }
                else if (process.Status is ProcessingStatus.Completed or ProcessingStatus.Failed)
                {
                    _logger.LogInformation(
                        "Ignoring duplicate analysis admission for terminal process. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}, Status={Status}",
                        process.Id,
                        process.AnalysisRequestId,
                        process.Status);

                    activity?.SetStatus(ActivityStatusCode.Ok);

                    return Result.Success(new StartAnalysisProcessingResult(
                        process.Id,
                        process.AnalysisRequestId.Value,
                        process.Status,
                        process.StartedAtUtc ?? nowUtc,
                        process.CompletedAtUtc,
                        process.FailedAtUtc,
                        process.FailureReason));
                }
            }

            if (startedDomainEvent is not null)
            {
                activity?.SetTag("messaging.event.type", startedDomainEvent.GetType().Name);
                activity?.SetTag("messaging.event.id", startedDomainEvent.EventId);

                var startedIntegrationEvent = _startedIntegrationEventMapper.Map(startedDomainEvent);

                activity?.SetTag("messaging.integration_event.type", startedIntegrationEvent.EventType);
                activity?.SetTag("messaging.integration_event.id", startedIntegrationEvent.EventId);
                activity?.SetTag("correlation.id", startedIntegrationEvent.CorrelationId);

                if (startedIntegrationEvent.CausationId is not null)
                    activity?.SetTag("causation.id", startedIntegrationEvent.CausationId.Value);

                await _eventPublisher.PublishAsync(startedIntegrationEvent, cancellationToken);
            }

            var executionRequestedEvent = _executionRequestedIntegrationEventFactory.Create(command, process.Id);

            activity?.SetTag("messaging.execution_request.type", executionRequestedEvent.EventType);
            activity?.SetTag("messaging.execution_request.id", executionRequestedEvent.EventId);

            await _eventPublisher.PublishAsync(executionRequestedEvent, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new StartAnalysisProcessingResult(
                process.Id,
                process.AnalysisRequestId.Value,
                process.Status,
                process.StartedAtUtc ?? nowUtc,
                process.CompletedAtUtc,
                process.FailedAtUtc,
                process.FailureReason));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "An unexpected error occurred while admitting analysis processing. AnalysisRequestId={AnalysisRequestId}",
                command.AnalysisRequestId);

            var errorCode = "processing.error";

            if (ex is ValidationException)
                errorCode = "processing.validation_error";
            else if (ex is DomainException)
                errorCode = "processing.domain_error";
            else if (ex is ArgumentException)
                errorCode = "processing.invalid_argument";

            return Result.Failure<StartAnalysisProcessingResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }

    private static DiagramType ResolveDiagramType(string contentType)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return DiagramType.Pdf;

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return DiagramType.Image;

        return DiagramType.Unknown;
    }
}
