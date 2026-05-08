using System.Text.Json.Serialization;

namespace Fiap.SecureAnalyzer.ApiGateway.Contracts.Responses;

public sealed record AnalysisDetailsResponse(
    [property: JsonPropertyName("analysis")] AnalysisStatusResponse Analysis,
    [property: JsonPropertyName("report")] ReportByAnalysisResponse? Report);
