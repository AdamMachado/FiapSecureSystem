using ProcessingService.Infrastructure.AI.Diagnostics;
using ProcessingService.Infrastructure.AI.OpenAI.Models;
using System.Text.Json;

namespace ProcessingService.Infrastructure.AI.OpenAI;

internal static class OpenAiResponseEnvelopeParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static OpenAiResponseEnvelope Parse(string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var dto = JsonSerializer.Deserialize<OpenAiResponseDto>(body, JsonOptions);

        if (dto is null)
        {
            return new OpenAiResponseEnvelope
            {
                RawBody = body,
                OutputText = string.Empty,
                Usage = null
            };
        }

        return new OpenAiResponseEnvelope
        {
            RawBody = body,
            Id = dto.Id,
            Instructions = dto.Instructions,
            Model = dto.Model,
            OutputText = ExtractOutputText(dto),
            Usage = MapUsage(dto.Usage)
        };
    }

    private static string ExtractOutputText(OpenAiResponseDto dto)
    {
        if (dto.Output is null)
            return string.Empty;

        var outputText = dto.Output
            .Where(x => string.Equals(x.Type, "message", StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Content ?? Array.Empty<OpenAiOutputContentDto>())
            .FirstOrDefault(x =>
                string.Equals(x.Type, "output_text", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(x.Text))
            ?.Text;

        if (!string.IsNullOrWhiteSpace(outputText))
            return outputText;

        return dto.Output
            .SelectMany(x => x.Content ?? Array.Empty<OpenAiOutputContentDto>())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Text))
            ?.Text ?? string.Empty;
    }

    private static AiUsageMetrics? MapUsage(OpenAiUsageDto? usage)
    {
        if (usage is null)
            return null;

        return new AiUsageMetrics(
            InputTokens: usage.InputTokens,
            CachedInputTokens: usage.InputTokensDetails?.CachedTokens,
            OutputTokens: usage.OutputTokens,
            ReasoningOutputTokens: usage.OutputTokensDetails?.ReasoningTokens,
            TotalTokens: usage.TotalTokens);
    }
}