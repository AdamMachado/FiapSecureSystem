using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

public sealed class ApiGatewayProblemDetails
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
