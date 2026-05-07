using UploadService.Domain.Entities;

namespace UploadService.Application.UseCases.Common;

internal static class AnalysisRequestMappings
{
    public static AnalysisRequestSummaryResult ToSummaryResult(this AnalysisRequest analysisRequest)
        => new(
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
