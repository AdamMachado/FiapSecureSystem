using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Infrastructure.AI.Diagnostics;
using ProcessingService.Infrastructure.AI.Exceptions;
using ProcessingService.Infrastructure.AI.Guardrails;
using ProcessingService.Infrastructure.AI.Inspection;
using ProcessingService.Infrastructure.AI.OpenAI.Models;
using ProcessingService.Infrastructure.AI.Options;
using ProcessingService.Infrastructure.AI.Policies;
using ProcessingService.Infrastructure.AI.Validation;

namespace ProcessingService.Infrastructure.AI.OpenAI;

public sealed class OpenAiArchitectureAnalyzer : IArchitectureAnalyzer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        return options;
    }

    private readonly ArchitectureAnalysisInputValidator _inputValidator;
    private readonly IEnumerable<IAnalysisFileInspector> _inspectors;
    private readonly AiInputCostPolicy _costPolicy;
    private readonly AiServiceTierPolicy _serviceTierPolicy;
    private readonly ArchitectureAnalysisPromptBuilder _promptBuilder;
    private readonly OpenAiArchitectureAnalysisClient _client;
    private readonly ArchitectureAnalysisSanitizer _sanitizer;
    private readonly ArchitectureAnalysisOutputValidator _outputValidator;
    private readonly ArchitectureAnalysisResponseMapper _mapper;
    private readonly AiUsageLogger _usageLogger;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<OpenAiArchitectureAnalyzer> _logger;

    public OpenAiArchitectureAnalyzer(
        ArchitectureAnalysisInputValidator inputValidator,
        IEnumerable<IAnalysisFileInspector> inspectors,
        AiInputCostPolicy costPolicy,
        AiServiceTierPolicy serviceTierPolicy,
        ArchitectureAnalysisPromptBuilder promptBuilder,
        OpenAiArchitectureAnalysisClient client,
        ArchitectureAnalysisSanitizer sanitizer,
        ArchitectureAnalysisOutputValidator outputValidator,
        ArchitectureAnalysisResponseMapper mapper,
        AiUsageLogger usageLogger,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<OpenAiArchitectureAnalyzer> logger)
    {
        _inputValidator = inputValidator;
        _inspectors = inspectors;
        _costPolicy = costPolicy;
        _serviceTierPolicy = serviceTierPolicy;
        _promptBuilder = promptBuilder;
        _client = client;
        _sanitizer = sanitizer;
        _outputValidator = outputValidator;
        _mapper = mapper;
        _usageLogger = usageLogger;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public bool CanHandle(DiagramType diagramType)
    {
        return diagramType is DiagramType.Pdf or DiagramType.Image;
    }

    public async Task<ArchitectureAnalysisResult> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting architecture analysis. AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}, ContentType={ContentType}",
            request.AnalysisRequestId,
            request.DiagramType,
            request.ContentType);

        if (!CanHandle(request.DiagramType))
        {
            _logger.LogError(
                "Unsupported diagram type for analysis. AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}",
                request.AnalysisRequestId,
                request.DiagramType);

            throw new UnsupportedDiagramFormatException(request.ContentType);
        }

        var content = await ReadContentAsync(request.Content, cancellationToken);
        _inputValidator.ValidateAndThrow(request, content);

        _logger.LogInformation(
            "Content read successfully for analysis. AnalysisRequestId={AnalysisRequestId}, ContentSize={ContentSize} bytes",
            request.AnalysisRequestId,
            content.Length);

        var inspector = _inspectors.FirstOrDefault(x => x.CanInspect(request.DiagramType));
        
        if (inspector is null)
        {
            _logger.LogError(
                "No suitable inspector found for diagram type. AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}",
                request.AnalysisRequestId,
                request.DiagramType);

            throw new UnsupportedDiagramFormatException(request.ContentType);
        }

        var inspection = await inspector.InspectAsync(request, content, cancellationToken);
        _costPolicy.ValidateAndThrow(inspection);

        _logger.LogInformation(
            "Inspection completed for analysis. AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}, InspectionDetails={InspectionDetails}",
            request.AnalysisRequestId,
            request.DiagramType,
            inspection);

        var developerInstructions = _promptBuilder.BuildDeveloperInstructions();
        var userInstructions = _promptBuilder.BuildUserInstructions(request, inspection);

        var serviceTier = _serviceTierPolicy.Normalize(_openAiOptions.ServiceTier);

        try
        {
            return await AnalyzeWithTierAsync(
                request,
                inspection,
                content,
                developerInstructions,
                userInstructions,
                serviceTier,
                cancellationToken);
        }
        catch (ExternalAiUnavailableException ex) when (
            _openAiOptions.EnableFallbackToDefaultServiceTier &&
            serviceTier.Equals("flex", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackTier = _serviceTierPolicy.GetFallbackTier(serviceTier);

            _logger.LogWarning(
                ex,
                "AI analysis failed using service tier '{ServiceTier}'. Retrying with fallback tier '{FallbackTier}'. AnalysisRequestId={AnalysisRequestId}",
                serviceTier,
                fallbackTier,
                request.AnalysisRequestId);

            return await AnalyzeWithTierAsync(
                request,
                inspection,
                content,
                developerInstructions,
                userInstructions,
                fallbackTier,
                cancellationToken);
        }
    }

    private async Task<ArchitectureAnalysisResult> AnalyzeWithTierAsync(
        ArchitectureAnalysisRequest request,
        AnalysisFileInspectionResult inspection,
        byte[] content,
        string developerInstructions,
        string userInstructions,
        string serviceTier,
        CancellationToken cancellationToken)
    {
        var envelope = await _client.AnalyzeAsync(
            request,
            inspection,
            content,
            developerInstructions,
            userInstructions,
            serviceTier,
            cancellationToken);

        var parsed = DeserializeResponse(envelope.OutputText);
        var sanitized = _sanitizer.Sanitize(parsed);
        _outputValidator.ValidateAndThrow(sanitized);

        _usageLogger.LogCompleted(
            request.AnalysisRequestId,
            "OpenAI",
            _openAiOptions.Model,
            serviceTier,
            envelope.Usage);

        return _mapper.Map(sanitized);
    }

    private static ArchitectureAnalysisResponse DeserializeResponse(string outputText)
    {
        try
        {
            var response = JsonSerializer.Deserialize<ArchitectureAnalysisResponse>(outputText, JsonSerializerOptions);
            return response ?? throw new AiResponseValidationException("The AI response JSON is empty.");
        }
        catch (JsonException ex)
        {
            throw new AiResponseValidationException(
                $"The AI response is not valid JSON for the expected schema." +
                $"\nDetails: {ex.Message}" +
                $"\nOutput Response: {outputText}");
        }
    }

    private static async Task<byte[]> ReadContentAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content is null)
            throw new DiagramProcessingException("The source file content stream is required.");

        if (!content.CanRead)
            throw new DiagramProcessingException("The source file content stream is not readable.");

        if (content.CanSeek)
            content.Position = 0;

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }
}
