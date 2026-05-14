using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

public sealed class ReportByAnalysisResponse
{
    [JsonPropertyName("reportId")]
    public Guid ReportId { get; set; }

    [JsonPropertyName("analysisRequestId")]
    public Guid AnalysisRequestId { get; set; }

    [JsonPropertyName("requestedByUserId")]
    public Guid RequestedByUserId { get; set; }

    [JsonPropertyName("analysisData")]
    public JsonElement AnalysisData { get; set; }

    [JsonPropertyName("files")]
    public IReadOnlyCollection<AnalysisReportFileResponse> Files { get; set; } = Array.Empty<AnalysisReportFileResponse>();

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
