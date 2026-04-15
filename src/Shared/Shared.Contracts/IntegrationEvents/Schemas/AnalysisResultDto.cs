namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record AnalysisResultDto(
    IReadOnlyCollection<IdentifiedComponentDto> Components,
    IReadOnlyCollection<ArchitecturalRiskDto> Risks,
    IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations,
    AnalysisSummaryDto Summary);