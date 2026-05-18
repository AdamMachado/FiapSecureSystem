using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.ApiGateway.Options;

public sealed class IdentityServiceOptions
{
    public const string SectionName = "ExternalServices:IdentityService";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
