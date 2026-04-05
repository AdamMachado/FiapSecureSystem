using Shared.Contracts.Enums;

namespace Shared.Contracts.Models;

public sealed record AnalysisRiskItem(
    string Title,
    string Description,
    RiskSeverity Severity,
    ComponentType Component);