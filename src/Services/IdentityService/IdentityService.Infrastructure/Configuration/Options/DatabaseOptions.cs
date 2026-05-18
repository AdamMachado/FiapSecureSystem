namespace IdentityService.Infrastructure.Configuration.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;
    public string Schema { get; init; } = "identity";
    public bool EnableSensitiveDataLogging { get; init; }
}
