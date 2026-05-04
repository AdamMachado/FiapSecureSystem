using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Infrastructure.AI.OpenAI.Models;
using Shared.Contracts.IntegrationEvents.Schemas;
using System.ComponentModel;

namespace ProcessingService.Infrastructure.AI.OpenAI;

public sealed class ArchitectureAnalysisResponseMapper
{
    internal ArchitectureAnalysisResult Map(ArchitectureAnalysisResponse response)
    {
        var components = response.Components
            .Select(static component =>
            {
                var metadata = component.Metadata == null || component.Metadata.Count == 0
                    ? null
                    : component.Metadata.ToDictionary(x => x.Key, x => x.Value);

                return new IdentifiedComponentDto(
                    component.Id,
                    component.Name,
                    component.Type,
                    component.Description,
                    component.Tags,
                    component.ConnectedTo,
                    metadata);
            })
            .ToArray();

        var risks = response.Risks
            .Select(static risk => new ArchitecturalRiskDto(
                risk.Id,
                risk.Title,
                risk.Description,
                risk.Severity,
                risk.AffectedComponentId,
                risk.AffectedComponentName,
                risk.Impact,
                risk.Likelihood,
                risk.Evidence))
            .ToArray();

        var recommendations = response.Recommendations
            .Select(static recommendation => new ArchitecturalRecommendationDto(
                recommendation.Id,
                recommendation.Title,
                recommendation.Description,
                recommendation.Category,
                recommendation.Priority,
                recommendation.RelatedRiskId,
                recommendation.TargetComponentId,
                recommendation.ExpectedBenefits))
            .ToArray();

        return new ArchitectureAnalysisResult(
            components,
            risks,
            recommendations,
            response.ExtractedText,
            response.Overview,
            response.RequiresManualReview,
            response.Warnings);
    }
}
