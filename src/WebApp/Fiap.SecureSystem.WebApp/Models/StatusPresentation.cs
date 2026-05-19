namespace Fiap.SecureSystem.WebApp.Models;

public static class StatusPresentation
{
    public static string ToCssClass(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "pending" => "received",
            "completed" or "analyzed" or "done" or "success" => "completed",
            "failed" or "error" => "failed",
            _ => "processing"
        };

    public static bool HasReport(string? status) =>
        status?.Trim().ToLowerInvariant() is "completed" or "analyzed" or "done" or "success";

    public static bool IsPendingOrProcessing(string? status) =>
        ToCssClass(status) is "received" or "processing";

    public static string ToDisplayLabel(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "completed" => "Analisado",
            "analyzed" => "Analisado",
            "done" => "Analisado",
            "failed" => "Erro",
            "error" => "Erro",
            "processing" => "Processando",
            "received" => "Recebido",
            "pending" => "Recebido",
            _ => string.IsNullOrWhiteSpace(status) ? "Desconhecido" : status
        };
}
