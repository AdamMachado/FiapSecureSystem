using System.Text.Json.Serialization;

namespace Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway.Contracts;

public sealed class AnalysisDetailsResponse
{
    [JsonPropertyName("analysis")]
    public AnalysisStatusResponse Analysis { get; set; } = new();

    [JsonPropertyName("report")]
    public ReportByAnalysisResponse? Report { get; set; }
}
