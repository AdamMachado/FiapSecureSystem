using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.UpdateAnalysisStatus;

public sealed record UpdateAnalysisStatusResult(
    Guid AnalysisRequestId,
    AnalysisStatus PreviousStatus,
    AnalysisStatus CurrentStatus,
    DateTime UpdatedAtUtc,
    string? FailureReason);