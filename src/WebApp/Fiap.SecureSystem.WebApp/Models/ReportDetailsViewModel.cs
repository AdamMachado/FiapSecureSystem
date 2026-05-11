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
    public string AnalysisDataJson { get; init; } = "{}";
    public IReadOnlyCollection<ReportFileViewModel> Files { get; init; } = Array.Empty<ReportFileViewModel>();
    public string? ErrorMessage { get; init; }
}
