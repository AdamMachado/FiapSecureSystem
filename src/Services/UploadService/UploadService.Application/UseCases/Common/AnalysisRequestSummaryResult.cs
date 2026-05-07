using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.Common;

public sealed record AnalysisRequestSummaryResult(
    Guid AnalysisRequestId,
    AnalysisStatus Status,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason);
