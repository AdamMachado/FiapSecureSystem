using Shared.Contracts.IntegrationEvents.Enums;

namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record ArchitecturalRecommendationDto(
    string Id,
    string Title,
    string Description,
    RecommendationCategory Category,
    RiskSeverityLevel Priority,
    string? RelatedRiskId,
    string? TargetComponentId,
    IReadOnlyCollection<string> ExpectedBenefits);
