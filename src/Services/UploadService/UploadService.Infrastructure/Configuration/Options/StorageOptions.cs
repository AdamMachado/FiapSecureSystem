namespace UploadService.Infrastructure.Configuration.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string BucketName { get; init; } = "analysis-uploads";
    public bool AutoCreateBucket { get; init; } = true;
}
