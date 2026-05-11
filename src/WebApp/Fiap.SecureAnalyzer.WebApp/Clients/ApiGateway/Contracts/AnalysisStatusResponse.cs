using System.Text.Json.Serialization;

namespace Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway.Contracts;

public sealed class AnalysisStatusResponse
{
    [JsonPropertyName("analysisRequestId")]
    public Guid AnalysisRequestId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("sizeInBytes")]
    public long SizeInBytes { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTime? StartedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [JsonPropertyName("failedAtUtc")]
    public DateTime? FailedAtUtc { get; set; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }
}
