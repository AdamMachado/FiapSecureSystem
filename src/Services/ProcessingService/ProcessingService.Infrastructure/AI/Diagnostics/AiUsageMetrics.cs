namespace ProcessingService.Infrastructure.AI.Diagnostics;

public sealed record AiUsageMetrics(
    int? InputTokens,
    int? CachedInputTokens,
    int? OutputTokens,
    int? ReasoningOutputTokens,
    int? TotalTokens);