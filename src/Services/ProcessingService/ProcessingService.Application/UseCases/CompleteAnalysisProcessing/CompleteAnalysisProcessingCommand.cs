using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Application.UseCases.CompleteAnalysisProcessing;

public sealed record CompleteAnalysisProcessingCommand(
    Guid AnalysisRequestId,
    string ExtractedText,
    IReadOnlyCollection<IdentifiedComponentDto> Components,
    IReadOnlyCollection<ArchitecturalRiskDto> Risks,
    IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations,
    string Overview,
    bool RequiresManualReview,
    IReadOnlyCollection<string> Warnings);
