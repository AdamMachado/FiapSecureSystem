using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.ApiGateway.Contracts.Requests;

public sealed class AnalysisIdsRequest
{
    [Required]
    [JsonPropertyName("analysisRequestIds")]
    public IReadOnlyCollection<Guid>? AnalysisRequestIds { get; set; }

    public AnalysisIdsRequest() { }

    public AnalysisIdsRequest(IReadOnlyCollection<Guid>? analysisRequestIds) {
        AnalysisRequestIds = analysisRequestIds;
    }
}
