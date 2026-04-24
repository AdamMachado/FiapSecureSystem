namespace ProcessingService.Domain.Enums;

public enum ComponentDiscoverySource
{
    Unknown = 0,
    TextExtraction = 1,
    VisionModel = 2,
    LlmInference = 3,
    RuleBased = 4,
    Hybrid = 5
}
