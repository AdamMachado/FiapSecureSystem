namespace Fiap.SecureSystem.WebApp.Models;

public sealed class ReportDetailsViewModel
{
    public Guid AnalysisId { get; init; }
    public Guid ReportId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public int ComponentCount { get; init; }
    public int RiskCount { get; init; }
    public int RecommendationCount { get; init; }
    public string SummaryOverview { get; init; } = string.Empty;
    public bool RequiresManualReview { get; init; }
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<ReportComponentViewModel> Components { get; init; } = Array.Empty<ReportComponentViewModel>();
    public IReadOnlyCollection<ReportRiskViewModel> Risks { get; init; } = Array.Empty<ReportRiskViewModel>();
    public IReadOnlyCollection<ReportRecommendationViewModel> Recommendations { get; init; } = Array.Empty<ReportRecommendationViewModel>();
    public IReadOnlyCollection<ReportFileViewModel> Files { get; init; } = Array.Empty<ReportFileViewModel>();
    public string? AssetDownloadUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ReportComponentViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ConnectedTo { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class ReportRiskViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string SeverityCssClass { get; init; } = "medium";
    public string AffectedComponentId { get; init; } = string.Empty;
    public string AffectedComponentName { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Likelihood { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
}

public sealed class ReportRecommendationViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string PriorityCssClass { get; init; } = "medium";
    public string RelatedRiskId { get; init; } = string.Empty;
    public string TargetComponentId { get; init; } = string.Empty;
    public IReadOnlyCollection<string> ExpectedBenefits { get; init; } = Array.Empty<string>();
}
