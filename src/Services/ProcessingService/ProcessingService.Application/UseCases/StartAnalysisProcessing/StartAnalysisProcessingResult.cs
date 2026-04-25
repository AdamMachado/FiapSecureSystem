using ProcessingService.Domain.Enums;

namespace ProcessingService.Application.UseCases.StartAnalysisProcessing;

public sealed record StartAnalysisProcessingResult(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    ProcessingStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason);
