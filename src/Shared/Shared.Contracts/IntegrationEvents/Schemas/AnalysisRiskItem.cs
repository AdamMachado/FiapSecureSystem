using Shared.Contracts.IntegrationEvents.Enums;

namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record AnalysisRiskItem(
    string Title,
    string Description,
    RiskSeverity Severity,
    ComponentType Component);