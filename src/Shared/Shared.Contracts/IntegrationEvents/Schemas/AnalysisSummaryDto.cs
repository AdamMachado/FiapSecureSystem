namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record AnalysisSummaryDto(
    string Overview,
    int TotalComponents,
    int TotalRisks,
    int TotalRecommendations,
    bool RequiresManualReview,
    IReadOnlyCollection<string> Warnings);
