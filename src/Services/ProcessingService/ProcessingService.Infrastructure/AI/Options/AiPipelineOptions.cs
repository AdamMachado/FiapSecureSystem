namespace ProcessingService.Infrastructure.AI.Options;

public sealed class AiPipelineOptions
{
    public const string SectionName = "AiPipeline";

    public bool UseStubAnalyzer { get; init; }
}