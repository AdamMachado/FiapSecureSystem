using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
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
    private readonly IObjectStorage _objectStorage;

    private readonly IEnumerable<IArchitectureAnalyzer> _architectureAnalyzers;

    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent> _startedIntegrationEventMapper;

    private readonly CompleteAnalysisProcessingHandler _completeHandler;
    private readonly FailAnalysisProcessingHandler _failHandler;

    private readonly ILogger<StartAnalysisProcessingHandler> _logger;
    private readonly ActivitySource _activitySource;

    public StartAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IObjectStorage objectStorage,
        IEnumerable<IArchitectureAnalyzer> architectureAnalyzers,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent> startedIntegrationEventMapper,
        CompleteAnalysisProcessingHandler completeHandler,
        FailAnalysisProcessingHandler failHandler,
        ILogger<StartAnalysisProcessingHandler> logger,
        ActivitySource activitySource)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _objectStorage = objectStorage;

        _architectureAnalyzers = architectureAnalyzers;

        _eventPublisher = eventPublisher;
        _startedIntegrationEventMapper = startedIntegrationEventMapper;

        _completeHandler = completeHandler;
        _failHandler = failHandler;

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

            var analysisExists = await _repository.ExistsByAnalysisRequestIdAsync(
                analysisRequestId,
                cancellationToken);

            activity?.SetTag("processing.process.already_exists", analysisExists);

            if (analysisExists)
            {
                const string errorMessage = "Analysis process already exists.";

                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("error.type", "processing.already_exists");
                activity?.SetTag("error.message", errorMessage);

                _logger.LogWarning(
                    "Analysis process already exists for AnalysisRequestId={AnalysisRequestId}. Cannot start a new process for the same request.",
                    command.AnalysisRequestId);

                return Result.Failure<StartAnalysisProcessingResult>(
                    Error.Conflict(
                        "processing.already_exists",
                        $"Analysis process for request '{command.AnalysisRequestId}' already exists."));
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var diagramType = ResolveDiagramType(command.ContentType);

            activity?.SetTag("analysis.diagram_type", diagramType.ToString());
            activity?.SetTag("processing.started_at_utc", nowUtc.ToString("O"));

            var process = AnalysisProcess.Create(
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

            _logger.LogInformation(
                "Started analysis process. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}",
                process.Id,
                process.AnalysisRequestId,
                process.DiagramType);

            var startedDomainEvent = process
                .DequeueDomainEvents()
                .OfType<AnalysisProcessingStartedDomainEvent>()
                .Single();

            activity?.SetTag("messaging.event.type", startedDomainEvent.GetType().Name);
            activity?.SetTag("messaging.event.id", startedDomainEvent.EventId);

            var startedIntegrationEvent = _startedIntegrationEventMapper.Map(startedDomainEvent);

            activity?.SetTag("messaging.integration_event.type", startedIntegrationEvent.EventType);
            activity?.SetTag("messaging.integration_event.id", startedIntegrationEvent.EventId);
            activity?.SetTag("correlation.id", startedIntegrationEvent.CorrelationId);

            if (startedIntegrationEvent.CausationId is not null)
            {
                activity?.SetTag("causation.id", startedIntegrationEvent.CausationId.Value);
            }

            await _eventPublisher.PublishAsync(startedIntegrationEvent, cancellationToken);

            var objectContent = await _objectStorage.DownloadAsync(
                new DownloadObjectRequest(command.StorageBucket, command.StorageObjectKey),
                cancellationToken);

            activity?.SetTag("storage.download.completed", true);
            activity?.SetTag("storage.download.content_length", objectContent.Content.Length);

            var analyzer = _architectureAnalyzers.FirstOrDefault(a => a.CanHandle(diagramType));

            activity?.SetTag("analysis.analyzer.found", analyzer is not null);
            activity?.SetTag("analysis.analyzer.type", analyzer?.GetType().Name);

            if (analyzer is null)
            {
                var failureReason = $"No architecture analyzer found for diagram type '{process.DiagramType}'.";

                activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                activity?.SetTag("error.type", "processing.analyzer_not_found");
                activity?.SetTag("error.message", failureReason);

                process.MarkAsFailed(
                    failureReason,
                    null,
                    nowUtc);

                activity?.SetTag("processing.current_status", process.Status.ToString());

                _logger.LogError(
                    "No architecture analyzer found for diagram type. AnalysisProcessId={AnalysisProcessId}, DiagramType={DiagramType}",
                    process.Id,
                    process.DiagramType);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _failHandler.HandleAsync(
                    new FailAnalysisProcessingCommand(
                        command.AnalysisRequestId,
                        failureReason),
                    cancellationToken);

                return Result.Failure<StartAnalysisProcessingResult>(
                    Error.Failure("processing.analyzer_not_found", "No compatible analyzer was found."));
            }

            var analysisResult = await analyzer.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    command.AnalysisRequestId,
                    command.RequestedByUserId,
                    diagramType,
                    command.FileName,
                    command.ContentType,
                    objectContent.Content),
                cancellationToken);

            activity?.SetTag("analysis.extracted_text.length", analysisResult.ExtractedText?.Length ?? 0);
            activity?.SetTag("analysis.components.count", analysisResult.Components.Count);
            activity?.SetTag("analysis.risks.count", analysisResult.Risks.Count);
            activity?.SetTag("analysis.recommendations.count", analysisResult.Recommendations.Count);
            activity?.SetTag("analysis.requires_manual_review", analysisResult.RequiresManualReview);
            activity?.SetTag("analysis.warnings.count", analysisResult.Warnings.Count);

            var completed = await _completeHandler.HandleAsync(
                new CompleteAnalysisProcessingCommand(
                    command.AnalysisRequestId,
                    analysisResult.ExtractedText,
                    analysisResult.Components,
                    analysisResult.Risks,
                    analysisResult.Recommendations,
                    analysisResult.Overview,
                    analysisResult.RequiresManualReview,
                    analysisResult.Warnings),
                cancellationToken);

            if (completed.IsFailure)
            {
                activity?.SetStatus(ActivityStatusCode.Error, completed.Error.Message);
                activity?.SetTag("error.type", completed.Error.Code);
                activity?.SetTag("error.message", completed.Error.Message);

                _logger.LogError(
                    "Failed to complete analysis processing after successful analysis. AnalysisRequestId={AnalysisRequestId}, Error: {ErrorCode} - {ErrorMessage}",
                    command.AnalysisRequestId,
                    completed.Error.Code,
                    completed.Error.Message);

                return Result.Failure<StartAnalysisProcessingResult>(completed.Error);
            }

            activity?.SetTag("processing.completed_at_utc", completed.Value.CompletedAtUtc.ToString("O"));
            activity?.SetTag("processing.final_status", completed.Value.Status.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Successfully completed analysis processing. AnalysisRequestId={AnalysisRequestId}",
                command.AnalysisRequestId);

            return Result.Success(new StartAnalysisProcessingResult(
                completed.Value.AnalysisProcessId,
                completed.Value.AnalysisRequestId,
                completed.Value.Status,
                nowUtc,
                completed.Value.CompletedAtUtc,
                null,
                null));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "An unexpected error occurred during analysis processing. AnalysisRequestId={AnalysisRequestId}",
                command.AnalysisRequestId);

            var failed = await _failHandler.HandleAsync(
                new FailAnalysisProcessingCommand(
                    command.AnalysisRequestId,
                    "An unexpected error occurred during analysis processing.",
                    ex.Message),
                cancellationToken);

            if (failed.IsFailure)
            {
                activity?.SetTag("processing.failure_handler.error_code", failed.Error.Code);
                activity?.SetTag("processing.failure_handler.error_message", failed.Error.Message);

                return Result.Failure<StartAnalysisProcessingResult>(failed.Error);
            }

            activity?.SetTag("processing.process.id", failed.Value.AnalysisProcessId);
            activity?.SetTag("processing.final_status", failed.Value.Status.ToString());
            activity?.SetTag("processing.failed_at_utc", failed.Value.FailedAtUtc.ToString("O"));
            activity?.SetTag("analysis.failure.reason", failed.Value.FailureReason);

            return Result.Success(new StartAnalysisProcessingResult(
                failed.Value.AnalysisProcessId,
                failed.Value.AnalysisRequestId,
                failed.Value.Status,
                _dateTimeProvider.UtcNow,
                null,
                failed.Value.FailedAtUtc,
                failed.Value.FailureReason));
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
