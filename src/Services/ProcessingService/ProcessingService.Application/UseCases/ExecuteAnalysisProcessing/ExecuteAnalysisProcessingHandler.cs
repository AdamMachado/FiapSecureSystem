using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;

public sealed class ExecuteAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly IEnumerable<IArchitectureAnalyzer> _architectureAnalyzers;
    private readonly CompleteAnalysisProcessingHandler _completeHandler;
    private readonly FailAnalysisProcessingHandler _failHandler;
    private readonly ILogger<ExecuteAnalysisProcessingHandler> _logger;
    private readonly ActivitySource _activitySource;

    public ExecuteAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IObjectStorage objectStorage,
        IEnumerable<IArchitectureAnalyzer> architectureAnalyzers,
        CompleteAnalysisProcessingHandler completeHandler,
        FailAnalysisProcessingHandler failHandler,
        ILogger<ExecuteAnalysisProcessingHandler> logger,
        ActivitySource activitySource)
    {
        _repository = repository;
        _objectStorage = objectStorage;
        _architectureAnalyzers = architectureAnalyzers;
        _completeHandler = completeHandler;
        _failHandler = failHandler;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<Result<ExecuteAnalysisProcessingResult>> HandleAsync(
        ExecuteAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ProcessingService execute analysis processing",
            ActivityKind.Internal);

        activity?.SetTag("analysis.process.id", command.AnalysisProcessId);
        activity?.SetTag("analysis.request.id", command.AnalysisRequestId);
        activity?.SetTag("analysis.requested_by_user.id", command.RequestedByUserId);
        activity?.SetTag("analysis.file.name", command.FileName);
        activity?.SetTag("analysis.file.content_type", command.ContentType);
        activity?.SetTag("storage.bucket", command.StorageBucket);
        activity?.SetTag("storage.object.key", command.StorageObjectKey);

        _logger.LogInformation(
            "Handling deferred analysis execution. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}",
            command.AnalysisProcessId,
            command.AnalysisRequestId);

        try
        {
            var process = await _repository.GetByAnalysisRequestIdAsync(
                AnalysisRequestId.Create(command.AnalysisRequestId),
                cancellationToken);

            if (process is null)
            {
                const string errorMessage = "Analysis process was not found for deferred execution.";

                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("error.type", "processing.not_found");
                activity?.SetTag("error.message", errorMessage);

                return Result.Failure<ExecuteAnalysisProcessingResult>(
                    Error.NotFound(
                        "processing.not_found",
                        $"Analysis process for request '{command.AnalysisRequestId}' was not found."));
            }

            activity?.SetTag("processing.current_status", process.Status.ToString());
            activity?.SetTag("analysis.diagram_type", process.DiagramType.ToString());

            if (process.Status == ProcessingStatus.Completed)
            {
                _logger.LogInformation(
                    "Skipping deferred execution because analysis is already completed. AnalysisRequestId={AnalysisRequestId}",
                    command.AnalysisRequestId);

                activity?.SetStatus(ActivityStatusCode.Ok);

                return Result.Success(new ExecuteAnalysisProcessingResult(
                    process.Id,
                    process.AnalysisRequestId.Value,
                    process.Status,
                    process.CompletedAtUtc,
                    process.FailedAtUtc,
                    process.FailureReason));
            }

            if (process.Status == ProcessingStatus.Failed)
            {
                _logger.LogInformation(
                    "Skipping deferred execution because analysis is already failed. AnalysisRequestId={AnalysisRequestId}",
                    command.AnalysisRequestId);

                activity?.SetStatus(ActivityStatusCode.Ok);

                return Result.Success(new ExecuteAnalysisProcessingResult(
                    process.Id,
                    process.AnalysisRequestId.Value,
                    process.Status,
                    process.CompletedAtUtc,
                    process.FailedAtUtc,
                    process.FailureReason));
            }

            if (process.Status != ProcessingStatus.Processing)
            {
                var failureMessage =
                    $"Analysis process '{process.Id}' is in status '{process.Status}' and cannot be executed.";

                activity?.SetStatus(ActivityStatusCode.Error, failureMessage);
                activity?.SetTag("error.type", "processing.invalid_status");
                activity?.SetTag("error.message", failureMessage);

                return Result.Failure<ExecuteAnalysisProcessingResult>(
                    Error.Conflict("processing.invalid_status", failureMessage));
            }

            var objectContent = await _objectStorage.DownloadAsync(
                new DownloadObjectRequest(command.StorageBucket, command.StorageObjectKey),
                cancellationToken);

            activity?.SetTag("storage.download.completed", true);
            activity?.SetTag("storage.download.content_length", objectContent.Content.Length);

            var analyzer = _architectureAnalyzers.FirstOrDefault(a => a.CanHandle(process.DiagramType));

            activity?.SetTag("analysis.analyzer.found", analyzer is not null);
            activity?.SetTag("analysis.analyzer.type", analyzer?.GetType().Name);

            if (analyzer is null)
            {
                var failureReason = $"No architecture analyzer found for diagram type '{process.DiagramType}'.";

                activity?.SetStatus(ActivityStatusCode.Error, failureReason);
                activity?.SetTag("error.type", "processing.analyzer_not_found");
                activity?.SetTag("error.message", failureReason);

                var failed = await _failHandler.HandleAsync(
                    new FailAnalysisProcessingCommand(
                        command.AnalysisRequestId,
                        failureReason),
                    cancellationToken);

                if (failed.IsFailure)
                    return Result.Failure<ExecuteAnalysisProcessingResult>(failed.Error);

                return Result.Success(new ExecuteAnalysisProcessingResult(
                    failed.Value.AnalysisProcessId,
                    failed.Value.AnalysisRequestId,
                    failed.Value.Status,
                    null,
                    failed.Value.FailedAtUtc,
                    failed.Value.FailureReason));
            }

            var analysisResult = await analyzer.AnalyzeAsync(
                new ArchitectureAnalysisRequest(
                    command.AnalysisRequestId,
                    command.RequestedByUserId,
                    process.DiagramType,
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
                    analysisResult.ExtractedText!,
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

                return Result.Failure<ExecuteAnalysisProcessingResult>(completed.Error);
            }

            activity?.SetTag("processing.completed_at_utc", completed.Value.CompletedAtUtc.ToString("O"));
            activity?.SetTag("processing.final_status", completed.Value.Status.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new ExecuteAnalysisProcessingResult(
                completed.Value.AnalysisProcessId,
                completed.Value.AnalysisRequestId,
                completed.Value.Status,
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
                "An unexpected error occurred during deferred analysis execution. AnalysisRequestId={AnalysisRequestId}",
                command.AnalysisRequestId);

            var failed = await _failHandler.HandleAsync(
                new FailAnalysisProcessingCommand(
                    command.AnalysisRequestId,
                    "An unexpected error occurred during analysis processing.",
                    ex.Message),
                cancellationToken);

            if (failed.IsFailure)
                return Result.Failure<ExecuteAnalysisProcessingResult>(failed.Error);

            activity?.SetTag("processing.final_status", failed.Value.Status.ToString());
            activity?.SetTag("processing.failed_at_utc", failed.Value.FailedAtUtc.ToString("O"));
            activity?.SetTag("analysis.failure.reason", failed.Value.FailureReason);

            return Result.Success(new ExecuteAnalysisProcessingResult(
                failed.Value.AnalysisProcessId,
                failed.Value.AnalysisRequestId,
                failed.Value.Status,
                null,
                failed.Value.FailedAtUtc,
                failed.Value.FailureReason));
        }
    }
}
