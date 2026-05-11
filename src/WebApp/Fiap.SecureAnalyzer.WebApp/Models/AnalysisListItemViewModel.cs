namespace Fiap.SecureAnalyzer.WebApp.Models;

public sealed class AnalysisListItemViewModel
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusCssClass { get; init; } = "processing";
    public bool HasReport { get; init; }
}
