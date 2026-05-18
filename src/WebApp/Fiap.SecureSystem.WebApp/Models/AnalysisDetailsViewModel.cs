namespace Fiap.SecureSystem.WebApp.Models;

public sealed class AnalysisDetailsViewModel
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusCssClass { get; init; } = "processing";
    public string ContentType { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? FailedAtUtc { get; init; }
    public string? FailureReason { get; init; }
    public bool HasReport { get; init; }
    public string? ReportDownloadUrl { get; init; }
    public string? ReportDetailsUrl { get; init; }
    public string? AssetDownloadUrl { get; init; }
}
