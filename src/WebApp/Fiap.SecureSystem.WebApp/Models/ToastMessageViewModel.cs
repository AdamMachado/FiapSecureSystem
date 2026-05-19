namespace Fiap.SecureSystem.WebApp.Models;

public sealed class ToastMessageViewModel
{
    public string Type { get; init; } = "info";

    public string Message { get; init; } = string.Empty;

    public string? Title { get; init; }

    public bool IsSticky { get; init; }

    public int DurationMs { get; init; } = 5000;
}
