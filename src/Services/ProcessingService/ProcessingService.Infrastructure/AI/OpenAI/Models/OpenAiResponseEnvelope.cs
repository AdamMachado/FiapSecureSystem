using ProcessingService.Infrastructure.AI.Diagnostics;

namespace ProcessingService.Infrastructure.AI.OpenAI.Models;

internal sealed class OpenAiResponseEnvelope
{
    public required string RawBody { get; init; }

    public string? Id { get; init; }

    public string? Instructions { get; init; }

    public string? Model { get; init; }

    public required string OutputText { get; init; }

    public AiUsageMetrics? Usage { get; init; }
}