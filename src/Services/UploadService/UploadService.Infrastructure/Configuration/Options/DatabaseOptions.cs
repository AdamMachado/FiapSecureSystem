namespace UploadService.Infrastructure.Configuration.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;
    public string Schema { get; init; } = "upload";
    public bool EnableSensitiveDataLogging { get; init; }
}
