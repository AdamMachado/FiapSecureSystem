namespace UploadService.Infrastructure.Configuration.Options;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxFileSizeInBytes { get; init; } = 10 * 1024 * 1024; //10 MB

    public IReadOnlyCollection<string> SupportedContentTypes { get; init; } =
        new[]
        {
            "application/pdf",
            "image/png",
            "image/jpeg",
            "image/jpg"
        };
}