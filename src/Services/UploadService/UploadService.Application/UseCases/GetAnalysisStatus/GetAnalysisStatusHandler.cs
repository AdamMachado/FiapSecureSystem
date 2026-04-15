using Shared.Kernel.Result;
using UploadService.Application.Abstractions.Persistence;

namespace UploadService.Application.UseCases.GetAnalysisStatus;

public sealed class GetAnalysisStatusHandler
{
    private readonly IAnalysisRequestRepository _repository;

    public GetAnalysisStatusHandler(IAnalysisRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetAnalysisStatusResult>> HandleAsync(
        GetAnalysisStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysisRequest = await _repository.GetByIdAsync(query.AnalysisRequestId, cancellationToken);

        if (analysisRequest is null) 
        {
            return Result.Failure<GetAnalysisStatusResult>(new Error(
                Code: "analysis_request.not_found",
                Message: $"Analysis request with Id '{query.AnalysisRequestId}' was not found."));
        }

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