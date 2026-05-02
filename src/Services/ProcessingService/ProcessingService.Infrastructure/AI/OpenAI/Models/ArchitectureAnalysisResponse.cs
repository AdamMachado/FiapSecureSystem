using Shared.Contracts.IntegrationEvents.Enums;

namespace ProcessingService.Infrastructure.AI.OpenAI.Models;

internal sealed record ArchitectureAnalysisResponse(
    IReadOnlyCollection<ArchitectureComponentResponse> Components,
    IReadOnlyCollection<ArchitectureRiskResponse> Risks,
    IReadOnlyCollection<ArchitectureRecommendationResponse> Recommendations,
    string ExtractedText,
    string Overview,
    bool RequiresManualReview,
    IReadOnlyCollection<string> Warnings);

internal sealed record ArchitectureComponentResponse(
    string Id,
    string Name,
    ComponentType Type,
    string? Description,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> ConnectedTo,
    IReadOnlyDictionary<string, string>? Metadata);

internal sealed record ArchitectureRiskResponse(
    string Id,
    string Title,
    string Description,
    RiskSeverityLevel Severity,
    string? AffectedComponentId,
    string? AffectedComponentName,
    string? Impact,
    string? Likelihood,
    IReadOnlyCollection<string> Evidence);

internal sealed record ArchitectureRecommendationResponse(
    string Id,
    string Title,
    string Description,
    RecommendationCategory Category,
    RiskSeverityLevel Priority,
    string? RelatedRiskId,
    string? TargetComponentId,
    IReadOnlyCollection<string> ExpectedBenefits);
