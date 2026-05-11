namespace Fiap.SecureSystem.WebApp.Models;

public static class StatusPresentation
{
    public static string ToCssClass(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "completed" or "analyzed" or "done" or "success" => "completed",
            "failed" or "error" => "error",
            _ => "processing"
        };

    public static bool HasReport(string? status) =>
        status?.Trim().ToLowerInvariant() is "completed" or "analyzed" or "done" or "success";

    public static string ToDisplayLabel(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "completed" => "Analisado",
            "analyzed" => "Analisado",
            "done" => "Analisado",
            "failed" => "Erro",
            "error" => "Erro",
            "processing" => "Processando",
            "pending" => "Recebido",
            _ => string.IsNullOrWhiteSpace(status) ? "Desconhecido" : status
        };
}
