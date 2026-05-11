namespace Fiap.SecureAnalyzer.WebApp.Models;

public sealed class DashboardViewModel
{
    public int TotalAnalyses { get; init; }
    public int ProcessingAnalyses { get; init; }
    public int CompletedAnalyses { get; init; }
    public int FailedAnalyses { get; init; }
    public IReadOnlyCollection<AnalysisListItemViewModel> RecentAnalyses { get; init; } = Array.Empty<AnalysisListItemViewModel>();
    public string? ErrorMessage { get; init; }
}
