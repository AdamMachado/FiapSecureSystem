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

    public StartAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IObjectStorage objectStorage,
        IEnumerable<IArchitectureAnalyzer> architectureAnalyzers,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent> startedIntegrationEventMapper,
        CompleteAnalysisProcessingHandler completeHandler,
        FailAnalysisProcessingHandler failHandler)
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
    }

    public async Task<Result<StartAnalysisProcessingResult>> HandleAsync(
        StartAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await _repository.ExistsByAnalysisRequestIdAsync(command.AnalysisRequestId, cancellationToken))
        {
            return Result.Failure<StartAnalysisProcessingResult>(
                Error.Conflict(
                    "processing.already_exists",
                    $"Analysis process for request '{command.AnalysisRequestId}' already exists."));
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var diagramType = ResolveDiagramType(command.ContentType);

        var process = AnalysisProcess.Create(
            Guid.NewGuid(),
            AnalysisRequestId.Create(command.AnalysisRequestId),
            command.RequestedByUserId,
            SourceFileLocation.Create(command.StorageBucket, command.StorageObjectKey),
            diagramType,
            nowUtc);

        process.MarkAsStarted(nowUtc);

        await _repository.AddAsync(process, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var startedDomainEvent = process
            .DequeueDomainEvents()
            .OfType<AnalysisProcessingStartedDomainEvent>()
            .Single();

        await _eventPublisher.PublishAsync(_startedIntegrationEventMapper.Map(startedDomainEvent), cancellationToken);

        try
        {
            var objectContent = await _objectStorage.DownloadAsync(
                new DownloadObjectRequest(command.StorageBucket, command.StorageObjectKey),
                cancellationToken);

            var analyzer = _architectureAnalyzers.FirstOrDefault(a => a.CanHandle(diagramType));

            if (analyzer is null)
            {
                process.MarkAsFailed(
                    $"No architecture analyzer found for diagram type '{process.DiagramType}'.",
                    null,
                    nowUtc);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var failed = await _failHandler.HandleAsync(
                    new FailAnalysisProcessingCommand(
                        command.AnalysisRequestId,
                        $"No architecture analyzer found for diagram type '{process.DiagramType}'."),
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
                return Result.Failure<StartAnalysisProcessingResult>(completed.Error);

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
            var failed = await _failHandler.HandleAsync(
                new FailAnalysisProcessingCommand(
                    command.AnalysisRequestId,
                    "An unexpected error occurred during analysis processing.",
                    ex.Message),
                cancellationToken);

            if (failed.IsFailure)
                return Result.Failure<StartAnalysisProcessingResult>(failed.Error);

            return Result.Success(new StartAnalysisProcessingResult(
                failed.Value.AnalysisProcessId,
                failed.Value.AnalysisRequestId,
                failed.Value.Status,
                nowUtc,
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
