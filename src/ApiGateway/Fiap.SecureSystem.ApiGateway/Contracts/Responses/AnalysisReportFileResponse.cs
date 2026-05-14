using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.ApiGateway.Contracts.Responses;

public sealed record AnalysisReportFileResponse(
    [property: JsonPropertyName("format")] ReportFileFormat Format,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("bucketName")] string BucketName,
    [property: JsonPropertyName("objectKey")] string ObjectKey,
    [property: JsonPropertyName("generatedAtUtc")] DateTime GeneratedAtUtc);
