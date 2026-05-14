using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.WebApp.Options;

public sealed class ApiGatewayOptions
{
    public const string SectionName = "ApiGateway";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
