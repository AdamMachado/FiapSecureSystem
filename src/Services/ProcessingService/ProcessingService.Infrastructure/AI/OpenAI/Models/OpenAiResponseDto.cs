using System.Text.Json.Serialization;

namespace ProcessingService.Infrastructure.AI.OpenAI;

internal sealed class OpenAiResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("instructions")]
    public string? Instructions { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("output")]
    public IReadOnlyCollection<OpenAiOutputItemDto>? Output { get; init; }

    [JsonPropertyName("usage")]
    public OpenAiUsageDto? Usage { get; init; }
}

internal sealed class OpenAiOutputItemDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("content")]
    public IReadOnlyCollection<OpenAiOutputContentDto>? Content { get; init; }
}

internal sealed class OpenAiOutputContentDto
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

internal sealed class OpenAiUsageDto
{
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; init; }

    [JsonPropertyName("input_tokens_details")]
    public OpenAiInputTokensDetailsDto? InputTokensDetails { get; init; }

    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; init; }

    [JsonPropertyName("output_tokens_details")]
    public OpenAiOutputTokensDetailsDto? OutputTokensDetails { get; init; }

    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; init; }
}

internal sealed class OpenAiInputTokensDetailsDto
{
    [JsonPropertyName("cached_tokens")]
    public int? CachedTokens { get; init; }
}

internal sealed class OpenAiOutputTokensDetailsDto
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; init; }
}