namespace IdentityService.Infrastructure.Configuration.Options;

public sealed class SeedUserOptions
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
}
