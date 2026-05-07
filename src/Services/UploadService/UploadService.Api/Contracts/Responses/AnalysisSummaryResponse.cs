using System.Text.Json.Serialization;

namespace UploadService.Api.Contracts.Responses;

public sealed record AnalysisSummaryResponse(
    [property: JsonPropertyName("analysisRequestId")] Guid AnalysisRequestId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("sizeInBytes")] long SizeInBytes,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc,
    [property: JsonPropertyName("startedAtUtc")] DateTime? StartedAtUtc,
    [property: JsonPropertyName("completedAtUtc")] DateTime? CompletedAtUtc,
    [property: JsonPropertyName("failedAtUtc")] DateTime? FailedAtUtc,
    [property: JsonPropertyName("failureReason")] string? FailureReason);
