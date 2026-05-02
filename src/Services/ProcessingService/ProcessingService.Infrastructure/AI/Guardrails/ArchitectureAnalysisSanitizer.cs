using Microsoft.Extensions.Options;
using ProcessingService.Infrastructure.AI.OpenAI.Models;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.AI.Guardrails;

public sealed class ArchitectureAnalysisSanitizer
{
    private readonly ArchitectureAnalysisOptions _options;

    public ArchitectureAnalysisSanitizer(IOptions<ArchitectureAnalysisOptions> options)
    {
        _options = options.Value;
    }

    internal ArchitectureAnalysisResponse Sanitize(ArchitectureAnalysisResponse response)
    {
        var components = response.Components
            .Where(static x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Name))
            .Take(_options.MaxComponents)
            .Select(x => x with
            {
                Id = NormalizeId(x.Id),
                Name = NormalizeText(x.Name, 200),
                Description = NormalizeNullableText(x.Description, _options.MaxDescriptionLength),
                Tags = NormalizeList(x.Tags, 20, 80),
                ConnectedTo = NormalizeList(x.ConnectedTo, 50, 80),
                Metadata = NormalizeMetadata(x.Metadata)
            })
            .ToArray();

        var componentIds = components.Select(static x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var risks = response.Risks
            .Where(static x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Title))
            .Take(_options.MaxRisks)
            .Select(x => x with
            {
                Id = NormalizeId(x.Id),
                Title = NormalizeText(x.Title, 200),
                Description = NormalizeText(x.Description, _options.MaxDescriptionLength),
                AffectedComponentId = NormalizeReference(x.AffectedComponentId, componentIds),
                AffectedComponentName = NormalizeNullableText(x.AffectedComponentName, 200),
                Impact = NormalizeNullableText(x.Impact, 600),
                Likelihood = NormalizeNullableText(x.Likelihood, 300),
                Evidence = NormalizeList(x.Evidence, 10, 300)
            })
            .ToArray();

        var riskIds = risks.Select(static x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recommendations = response.Recommendations
            .Where(static x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Title))
            .Take(_options.MaxRecommendations)
            .Select(x => x with
            {
                Id = NormalizeId(x.Id),
                Title = NormalizeText(x.Title, 200),
                Description = NormalizeText(x.Description, _options.MaxDescriptionLength),
                RelatedRiskId = NormalizeReference(x.RelatedRiskId, riskIds),
                TargetComponentId = NormalizeReference(x.TargetComponentId, componentIds),
                ExpectedBenefits = NormalizeList(x.ExpectedBenefits, 10, 250)
            })
            .ToArray();

        return response with
        {
            Components = components,
            Risks = risks,
            Recommendations = recommendations,
            ExtractedText = NormalizeText(response.ExtractedText ?? string.Empty, 12_000),
            Overview = NormalizeText(response.Overview, _options.MaxDescriptionLength),
            Warnings = NormalizeList(response.Warnings, _options.MaxWarnings, 300)
        };
    }

    private static string NormalizeId(string value) => NormalizeText(value, 80).Replace(" ", "-", StringComparison.Ordinal);

    private static string NormalizeText(string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeNullableText(string? value, int maxLength)
    {
        var normalized = NormalizeText(value, maxLength);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static IReadOnlyCollection<string> NormalizeList(IReadOnlyCollection<string>? values, int maxItems, int maxLength)
    {
        return (values ?? Array.Empty<string>())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(x => NormalizeText(x, maxLength))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string>? NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
            return null;

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .Take(20)
            .ToDictionary(
                pair => NormalizeText(pair.Key, 80),
                pair => NormalizeText(pair.Value, 200),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeReference(string? id, ISet<string> validIds)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var normalized = NormalizeId(id);
        return validIds.Contains(normalized) ? normalized : null;
    }
}
