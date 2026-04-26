using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Infrastructure.AI.Diagnostics;
using ProcessingService.Infrastructure.AI.Exceptions;
using ProcessingService.Infrastructure.AI.Inspection;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.AI.OpenAI;

public sealed class OpenAiArchitectureAnalysisClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ArchitectureAnalysisOptions _analysisOptions;
    private readonly ILogger<OpenAiArchitectureAnalysisClient> _logger;

    public OpenAiArchitectureAnalysisClient(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        IOptions<ArchitectureAnalysisOptions> analysisOptions,
        ILogger<OpenAiArchitectureAnalysisClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _analysisOptions = analysisOptions.Value;
        _logger = logger;
    }

    internal async Task<OpenAiResponseEnvelope> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        AnalysisFileInspectionResult inspection,
        byte[] content,
        string developerInstructions,
        string userInstructions,
        string serviceTier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new AiProviderConfigurationException("OpenAI API key is not configured.");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildResponsesEndpoint());
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.OrganizationId))
            httpRequest.Headers.Add("OpenAI-Organization", _options.OrganizationId);

        if (!string.IsNullOrWhiteSpace(_options.ProjectId))
            httpRequest.Headers.Add("OpenAI-Project", _options.ProjectId);

        var payload = BuildPayload(
            request,
            inspection,
            content,
            developerInstructions,
            userInstructions,
            serviceTier);

        httpRequest.Content = JsonContent.Create(payload, options: JsonSerializerOptions);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(_options.TimeoutSeconds, 30)));

        using var response = await _httpClient.SendAsync(httpRequest, timeout.Token);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);

        var outputText = ExtractOutputText(body);
        if (string.IsNullOrWhiteSpace(outputText))
        {
            _logger.LogWarning("OpenAI response did not include output text. RawBody={RawBody}", body);
            throw new ExternalAiUnavailableException("OpenAI response did not include output text.");
        }

        return new OpenAiResponseEnvelope(outputText, ExtractUsage(body));
    }

    private Uri BuildResponsesEndpoint()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/responses", UriKind.Absolute);
    }

    private object BuildPayload(
        ArchitectureAnalysisRequest request,
        AnalysisFileInspectionResult inspection,
        byte[] content,
        string developerInstructions,
        string userInstructions,
        string serviceTier)
    {
        var fileData = Convert.ToBase64String(content);
        var contentType = request.ContentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : request.ContentType;

        object fileInput = request.DiagramType == DiagramType.Pdf
            ? new
            {
                type = "input_file",
                filename = request.SourceFileName,
                file_data = $"data:{contentType};base64,{fileData}"
            }
            : new
            {
                type = "input_image",
                image_url = $"data:{contentType};base64,{fileData}",
                detail = _analysisOptions.ImageDetail
            };

        var userContent = new object[]
        {
            new { type = "input_text", text = userInstructions },
            fileInput
        };

        var schema = JsonSerializer.Deserialize<object>(ArchitectureAnalysisSchema.Json, JsonSerializerOptions)
            ?? throw new AiProviderConfigurationException("Architecture analysis JSON schema is invalid.");

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["instructions"] = developerInstructions,
            ["input"] = new object[]
            {
                new
                {
                    role = "user",
                    content = userContent
                }
            },
            ["max_output_tokens"] = _options.MaxOutputTokens,
            ["store"] = _options.StoreResponses,
            ["text"] = new
            {
                format = new
                {
                    type = "json_schema",
                    name = ArchitectureAnalysisSchema.Name,
                    schema,
                    strict = true
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(serviceTier) && !serviceTier.Equals("default", StringComparison.OrdinalIgnoreCase))
            payload["service_tier"] = serviceTier;

        return payload;
    }

    private static Exception CreateProviderException(HttpStatusCode statusCode, string body)
    {
        var reason = statusCode switch
        {
            HttpStatusCode.TooManyRequests => "OpenAI rate limit was reached.",
            HttpStatusCode.RequestTimeout => "OpenAI request timed out.",
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => "OpenAI service is temporarily unavailable.",
            HttpStatusCode.BadRequest => "OpenAI rejected the request payload.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "OpenAI authentication or authorization failed.",
            _ => $"OpenAI request failed with HTTP {(int)statusCode}."
        };

        return new ExternalAiUnavailableException($"{reason} Details: {TrimForException(body)}");
    }

    private static string TrimForException(string value)
    {
        return value.Length <= 1_000 ? value : value[..1_000];
    }

    private static string ExtractOutputText(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            return outputText.GetString() ?? string.Empty;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    return text.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static AiUsageMetrics? ExtractUsage(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
            return null;

        int? inputTokens = TryGetInt(usage, "input_tokens") ?? TryGetInt(usage, "prompt_tokens");
        int? outputTokens = TryGetInt(usage, "output_tokens") ?? TryGetInt(usage, "completion_tokens");
        int? totalTokens = TryGetInt(usage, "total_tokens");
        int? cachedTokens = null;

        if (usage.TryGetProperty("input_tokens_details", out var inputDetails) && inputDetails.ValueKind == JsonValueKind.Object)
            cachedTokens = TryGetInt(inputDetails, "cached_tokens");

        if (cachedTokens is null && usage.TryGetProperty("prompt_tokens_details", out var promptDetails) && promptDetails.ValueKind == JsonValueKind.Object)
            cachedTokens = TryGetInt(promptDetails, "cached_tokens");

        return new AiUsageMetrics(inputTokens, cachedTokens, outputTokens, totalTokens);
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }
}
