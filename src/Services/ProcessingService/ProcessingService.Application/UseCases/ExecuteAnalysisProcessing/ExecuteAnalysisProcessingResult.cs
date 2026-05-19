using ProcessingService.Domain.Enums;

namespace ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;

public sealed record ExecuteAnalysisProcessingResult(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    ProcessingStatus Status,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason);
