using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.WebApp.Options;

public sealed class IdentityServiceOptions
{
    public const string SectionName = "IdentityService";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
