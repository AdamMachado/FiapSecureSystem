using ProcessingService.Domain.Enums;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Schemas;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.Events;

public sealed class AnalysisProcessingCompletedDomainEvent : DomainEvent
{
    public AnalysisProcessingCompletedDomainEvent(
        Guid analysisProcessId,
        AnalysisRequestId analysisRequestId,
        Guid requestedByUserId,
        DiagramType diagramType,
        ExtractedText extractedText,
        IReadOnlyCollection<IdentifiedComponentDto> components,
        IReadOnlyCollection<ArchitecturalRiskDto> risks,
        IReadOnlyCollection<ArchitecturalRecommendationDto> recommendations,
        ProcessingResultSummary summary,
        DateTime completedAtUtc)
    {
        AnalysisProcessId = analysisProcessId;
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        DiagramType = diagramType;
        ExtractedText = extractedText;
        Components = components;
        Risks = risks;
        Recommendations = recommendations;
        Summary = summary;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid AnalysisProcessId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }
    public Guid RequestedByUserId { get; }
    public DiagramType DiagramType { get; }
    public ExtractedText ExtractedText { get; }
    public IReadOnlyCollection<IdentifiedComponentDto> Components { get; }
    public IReadOnlyCollection<ArchitecturalRiskDto> Risks { get; }
    public IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations { get; }
    public ProcessingResultSummary Summary { get; }
    public DateTime CompletedAtUtc { get; }
}
