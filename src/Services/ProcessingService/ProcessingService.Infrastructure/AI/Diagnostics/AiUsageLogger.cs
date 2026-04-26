using Microsoft.Extensions.Logging;

namespace ProcessingService.Infrastructure.AI.Diagnostics;

public sealed class AiUsageLogger
{
    private readonly ILogger<AiUsageLogger> _logger;

    public AiUsageLogger(ILogger<AiUsageLogger> logger)
    {
        _logger = logger;
    }

    public void LogCompleted(Guid analysisRequestId, string provider, string model, string serviceTier, AiUsageMetrics? usage)
    {
        if (usage is null)
        {
            _logger.LogInformation(
                "AI analysis completed without usage metadata. AnalysisRequestId={AnalysisRequestId}, Provider={Provider}, Model={Model}, ServiceTier={ServiceTier}",
                analysisRequestId,
                provider,
                model,
                serviceTier);

            return;
        }

        _logger.LogInformation(
            "AI analysis completed. AnalysisRequestId={AnalysisRequestId}, Provider={Provider}, Model={Model}, ServiceTier={ServiceTier}, InputTokens={InputTokens}, CachedInputTokens={CachedInputTokens}, OutputTokens={OutputTokens}, TotalTokens={TotalTokens}",
            analysisRequestId,
            provider,
            model,
            serviceTier,
            usage.InputTokens,
            usage.CachedInputTokens,
            usage.OutputTokens,
            usage.TotalTokens);
    }
}
