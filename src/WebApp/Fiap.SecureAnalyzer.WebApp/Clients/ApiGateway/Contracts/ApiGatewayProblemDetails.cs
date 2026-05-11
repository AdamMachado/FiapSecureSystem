using System.Text.Json.Serialization;

namespace Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway.Contracts;

public sealed class ApiGatewayProblemDetails
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
