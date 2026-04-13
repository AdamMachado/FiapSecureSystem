using UploadService.Application.UseCases.GetAnalysisStatus;
using UploadService.Domain.Entities;

namespace UploadService.Application.Mappings;

public static class AnalysisRequestMappings
{
    public static GetAnalysisStatusResult ToGetAnalysisStatusResult(this AnalysisRequest analysisRequest)
    {
        return new GetAnalysisStatusResult(
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
            analysisRequest.FailureReason);
    }
}