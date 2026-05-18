namespace IdentityService.Infrastructure.Configuration.Options;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public List<SeedUserOptions> SeedUsers { get; set; } = [];
}
