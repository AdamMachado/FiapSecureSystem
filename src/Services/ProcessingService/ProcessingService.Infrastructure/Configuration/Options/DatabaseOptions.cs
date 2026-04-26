namespace ProcessingService.Infrastructure.Configuration.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;
    public string Schema { get; init; } = "processing";
    public bool EnableSensitiveDataLogging { get; init; }
}
