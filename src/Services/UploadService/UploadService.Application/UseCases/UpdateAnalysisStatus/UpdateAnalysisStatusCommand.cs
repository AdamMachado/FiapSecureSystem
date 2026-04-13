using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.UpdateAnalysisStatus;

public sealed record UpdateAnalysisStatusCommand(
    Guid AnalysisRequestId,
    AnalysisStatus TargetStatus,
    string? FailureReason = null);