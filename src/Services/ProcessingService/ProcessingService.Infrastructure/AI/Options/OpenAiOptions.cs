namespace ProcessingService.Infrastructure.AI.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public string? OrganizationId { get; init; }
    public string? ProjectId { get; init; }
    public int MaxOutputTokens { get; init; } = 4_000;

    /// <summary>
    /// Supported values: auto, default, flex, priority. Use flex for async, low-priority workloads.
    /// </summary>
    public string ServiceTier { get; init; } = "auto";
    public bool EnableFallbackToDefaultServiceTier { get; init; } = true;
    public int TimeoutSeconds { get; init; } = 180;
    public int MaxRetries { get; init; } = 1;
    public bool StoreResponses { get; init; } = false;
}
