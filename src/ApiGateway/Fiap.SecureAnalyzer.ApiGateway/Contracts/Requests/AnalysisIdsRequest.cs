using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fiap.SecureAnalyzer.ApiGateway.Contracts.Requests;

public sealed record AnalysisIdsRequest(
    [property: Required]
    [property: JsonPropertyName("analysisRequestIds")]
    IReadOnlyCollection<Guid>? AnalysisRequestIds);
