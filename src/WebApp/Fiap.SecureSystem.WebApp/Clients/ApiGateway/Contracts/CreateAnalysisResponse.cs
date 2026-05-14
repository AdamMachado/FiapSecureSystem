using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

public sealed class CreateAnalysisResponse
{
    [JsonPropertyName("analysisRequestId")]
    public Guid AnalysisRequestId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("statusUrl")]
    public string StatusUrl { get; set; } = string.Empty;
}
