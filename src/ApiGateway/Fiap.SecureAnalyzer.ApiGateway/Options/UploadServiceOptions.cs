using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureAnalyzer.ApiGateway.Options;

public sealed class UploadServiceOptions
{
    public const string SectionName = "ExternalServices:UploadService";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
