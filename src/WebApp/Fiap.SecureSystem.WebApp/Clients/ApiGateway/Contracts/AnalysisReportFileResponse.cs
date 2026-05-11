using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

public sealed class AnalysisReportFileResponse
{
    [JsonPropertyName("format")]
    public ReportFileFormat Format { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("bucketName")]
    public string BucketName { get; set; } = string.Empty;

    [JsonPropertyName("objectKey")]
    public string ObjectKey { get; set; } = string.Empty;

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }
}
