using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.ApiGateway.Contracts.Responses;

public sealed record ReportByAnalysisResponse(
    [property: JsonPropertyName("reportId")] Guid ReportId,
    [property: JsonPropertyName("analysisRequestId")] Guid AnalysisRequestId,
    [property: JsonPropertyName("requestedByUserId")] Guid RequestedByUserId,
    [property: JsonPropertyName("analysisData")] JsonElement AnalysisData,
    [property: JsonPropertyName("files")] IReadOnlyCollection<AnalysisReportFileResponse> Files,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc);
