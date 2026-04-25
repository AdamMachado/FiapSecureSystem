using ProcessingService.Domain.Enums;

namespace ProcessingService.Application.UseCases.FailAnalysisProcessing;

public sealed record FailAnalysisProcessingResult(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    ProcessingStatus Status,
    DateTime FailedAtUtc,
    string FailureReason,
    string? FailureDetails);
