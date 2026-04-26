using Microsoft.Extensions.Options;
using ProcessingService.Infrastructure.AI.Exceptions;
using ProcessingService.Infrastructure.AI.OpenAI;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.AI.Guardrails;

public sealed class ArchitectureAnalysisOutputValidator
{
    private readonly ArchitectureAnalysisOptions _options;

    public ArchitectureAnalysisOutputValidator(IOptions<ArchitectureAnalysisOptions> options)
    {
        _options = options.Value;
    }

    internal void ValidateAndThrow(ArchitectureAnalysisResponse response)
    {
        if (response.Components is null || response.Components.Count == 0)
            throw new AiResponseValidationException("The AI response must include at least one identified component.");

        //TODO: Review Max Components behavior - Logging may be more appropriate than throwing an exception, depending on the use case and requirements.
        if (response.Components.Count > _options.MaxComponents)
            throw new AiResponseValidationException($"The AI response contains too many components. Maximum: {_options.MaxComponents}. Provided: {response.Components.Count}.");

        if (response.Risks is null)
            throw new AiResponseValidationException("The AI response risks collection cannot be null.");

        if (response.Recommendations is null)
            throw new AiResponseValidationException("The AI response recommendations collection cannot be null.");

        //TODO: Review max Risk and Recommendation behavior - Logging may be more appropriate than throwing an exception, depending on the use case and requirements.
        if (response.Risks.Count > _options.MaxRisks)
            throw new AiResponseValidationException($"The AI response contains too many risks. Maximum: {_options.MaxRisks}. Provided: {response.Risks.Count}.");

        if (response.Recommendations.Count > _options.MaxRecommendations)
            throw new AiResponseValidationException($"The AI response contains too many recommendations. Maximum: {_options.MaxRecommendations}. Provided: {response.Recommendations.Count}.");

        if (string.IsNullOrWhiteSpace(response.Overview))
            throw new AiResponseValidationException("The AI response overview is required.");

        EnsureUniqueIds(response.Components.Select(static x => x.Id), "component");
        EnsureUniqueIds(response.Risks.Select(static x => x.Id), "risk");
        EnsureUniqueIds(response.Recommendations.Select(static x => x.Id), "recommendation");

        var componentIds = response.Components
            .Select(static x => x.Id)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var risk in response.Risks)
        {
            if (string.IsNullOrWhiteSpace(risk.Title))
                throw new AiResponseValidationException($"Risk '{risk.Id}' must have a title.");

            if (_options.RequireEvidenceForRisks && (risk.Evidence is null || risk.Evidence.Count == 0))
                throw new AiResponseValidationException($"Risk '{risk.Id}' must include evidence.");

            if (!string.IsNullOrWhiteSpace(risk.AffectedComponentId) && !componentIds.Contains(risk.AffectedComponentId))
                throw new AiResponseValidationException($"Risk '{risk.Id}' references unknown component id '{risk.AffectedComponentId}'.");
        }

        var riskIds = response.Risks
            .Select(static x => x.Id)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var recommendation in response.Recommendations)
        {
            if (string.IsNullOrWhiteSpace(recommendation.Title))
                throw new AiResponseValidationException($"Recommendation '{recommendation.Id}' must have a title.");

            if (!string.IsNullOrWhiteSpace(recommendation.RelatedRiskId) && !riskIds.Contains(recommendation.RelatedRiskId))
                throw new AiResponseValidationException($"Recommendation '{recommendation.Id}' references unknown risk id '{recommendation.RelatedRiskId}'.");

            if (!string.IsNullOrWhiteSpace(recommendation.TargetComponentId) && !componentIds.Contains(recommendation.TargetComponentId))
                throw new AiResponseValidationException($"Recommendation '{recommendation.Id}' references unknown component id '{recommendation.TargetComponentId}'.");
        }
    }

    private static void EnsureUniqueIds(IEnumerable<string> ids, string entityName)
    {
        var duplicates = ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
            throw new AiResponseValidationException($"The AI response contains duplicate {entityName} ids: {string.Join(", ", duplicates)}.");
    }
}
