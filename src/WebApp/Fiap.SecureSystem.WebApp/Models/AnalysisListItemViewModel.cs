namespace Fiap.SecureSystem.WebApp.Models;

public sealed class AnalysisListItemViewModel
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? FailedAtUtc { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusCssClass { get; init; } = "processing";
    public bool HasReport { get; init; }
    public string DetailUrl { get; init; } = string.Empty;
    public string? ReportUrl { get; init; }
}
