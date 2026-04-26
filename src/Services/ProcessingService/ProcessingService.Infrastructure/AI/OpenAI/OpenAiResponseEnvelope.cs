using ProcessingService.Infrastructure.AI.Diagnostics;

namespace ProcessingService.Infrastructure.AI.OpenAI;

internal sealed record OpenAiResponseEnvelope(
    string OutputText,
    AiUsageMetrics? Usage);
