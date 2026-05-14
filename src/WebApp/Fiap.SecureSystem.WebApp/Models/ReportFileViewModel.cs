namespace Fiap.SecureSystem.WebApp.Models;

public sealed class ReportFileViewModel
{
    public string Format { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DateTime GeneratedAtUtc { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
}
