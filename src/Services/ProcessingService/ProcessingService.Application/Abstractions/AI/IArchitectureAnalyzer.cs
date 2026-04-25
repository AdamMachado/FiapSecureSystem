using ProcessingService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Application.Abstractions.AI;

public interface IArchitectureAnalyzer
{
    bool CanHandle(DiagramType diagramType);

    Task<ArchitectureAnalysisResult> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ArchitectureAnalysisRequest(
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    DiagramType DiagramType,
    string SourceFileName,
    string ContentType,
    Stream Content);

public sealed record ArchitectureAnalysisResult(
    IReadOnlyCollection<IdentifiedComponentDto> Components,
    IReadOnlyCollection<ArchitecturalRiskDto> Risks,
    IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations,
    string ExtractedText,
    string Overview,
    bool RequiresManualReview,
    IReadOnlyCollection<string> Warnings);
