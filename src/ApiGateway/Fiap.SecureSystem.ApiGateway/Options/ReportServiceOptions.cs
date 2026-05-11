using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.ApiGateway.Options;

public sealed class ReportServiceOptions
{
    public const string SectionName = "ExternalServices:ReportService";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
