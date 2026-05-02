using Microsoft.Extensions.Logging;
using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Persistence;

namespace UploadService.Application.UseCases.GetAnalysisStatus;

public sealed class GetAnalysisStatusHandler
{
    private readonly IAnalysisRequestRepository _repository;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<GetAnalysisStatusHandler> _logger;

    public GetAnalysisStatusHandler(
        IAnalysisRequestRepository repository,
        ActivitySource activitySource,
        ILogger<GetAnalysisStatusHandler> logger)
    {
        _repository = repository;
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<Result<GetAnalysisStatusResult>> HandleAsync(
        GetAnalysisStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "UploadService get analysis status",
            ActivityKind.Internal);

        activity?.SetTag("analysisRequestId", query.AnalysisRequestId);

        var analysisRequest = await _repository.GetByIdAsync(query.AnalysisRequestId, cancellationToken);

        if (analysisRequest is null) 
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Analysis request not found.");

            return Result.Failure<GetAnalysisStatusResult>(new Error(
                Code: "analysis_request.not_found",
                Message: $"Analysis request with Id '{query.AnalysisRequestId}' was not found."));
        }

        activity?.SetTag("analysisRequestStatus", analysisRequest.Status.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success(new GetAnalysisStatusResult(
           analysisRequest.Id,
           analysisRequest.Status,
           analysisRequest.FileMetadata.FileName,
           analysisRequest.FileMetadata.ContentType,
           analysisRequest.FileMetadata.SizeInBytes,
           analysisRequest.CreatedAtUtc,
           analysisRequest.UpdatedAtUtc,
           analysisRequest.StartedAtUtc,
           analysisRequest.CompletedAtUtc,
           analysisRequest.FailedAtUtc,
           analysisRequest.FailureReason));
    }
}