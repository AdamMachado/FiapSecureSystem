using Shared.Contracts.IntegrationEvents.Enums;

namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record ArchitecturalRiskDto(
    string Id,
    string Title,
    string Description,
    RiskSeverityLevel Severity,
    string? AffectedComponentId,
    string? AffectedComponentName,
    string? Impact,
    string? Likelihood,
    IReadOnlyCollection<string> Evidence);
