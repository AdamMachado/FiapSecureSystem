using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

public sealed class AnalysisIdsRequest
{
    [JsonPropertyName("analysisRequestIds")]
    public IReadOnlyCollection<Guid> AnalysisRequestIds { get; set; } = Array.Empty<Guid>();
}
