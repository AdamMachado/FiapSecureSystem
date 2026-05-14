using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.ApiGateway.Contracts.Responses;

public sealed record CreateAnalysisResponse(
    [property: JsonPropertyName("analysisRequestId")] Guid AnalysisRequestId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("statusUrl")] string StatusUrl);
