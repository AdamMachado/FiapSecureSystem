namespace ProcessingService.Infrastructure.Configuration.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Bucket used by ProcessingService for health checks or future internal artifacts.
    /// Source file buckets still come from AnalysisRequestedIntegrationEvent.
    /// </summary>
    public string BucketName { get; init; } = "analysis-uploads";
    public bool AutoCreateBucket { get; init; } = false;
}
