namespace UploadService.Infrastructure.Storage.MinIO;

public sealed class MinIoOptions
{
    public const string SectionName = "MinIo";

    public string Endpoint { get; init; } = "localhost:9000";
    public string AccessKey { get; init; } = "minioadmin";
    public string SecretKey { get; init; } = "minioadmin";
    public bool UseSsl { get; init; }
}
