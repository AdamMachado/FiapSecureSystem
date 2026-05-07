using System.Text.Json.Serialization;

namespace UploadService.Api.Contracts.Requests;

public sealed record GetAnalysisRequestsByIdsRequest(
    [property: JsonPropertyName("analysisRequestIds")] IReadOnlyCollection<Guid>? AnalysisRequestIds);
