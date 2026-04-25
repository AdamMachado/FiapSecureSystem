using ProcessingService.Domain.Enums;

namespace ProcessingService.Application.UseCases.CompleteAnalysisProcessing;

public sealed record CompleteAnalysisProcessingResult(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    ProcessingStatus Status,
    DateTime CompletedAtUtc,
    int TotalComponents,
    int TotalRisks,
    int TotalRecommendations);
