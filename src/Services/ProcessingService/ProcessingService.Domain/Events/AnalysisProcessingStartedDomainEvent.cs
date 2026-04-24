using ProcessingService.Domain.Enums;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.Events;

public sealed class AnalysisProcessingStartedDomainEvent : DomainEvent
{
    public AnalysisProcessingStartedDomainEvent(
        Guid analysisProcessId,
        AnalysisRequestId analysisRequestId,
        Guid requestedByUserId,
        SourceFileLocation sourceFileLocation,
        DiagramType diagramType,
        DateTime startedAtUtc)
    {
        AnalysisProcessId = analysisProcessId;
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        SourceFileLocation = sourceFileLocation;
        DiagramType = diagramType;
        StartedAtUtc = startedAtUtc;
    }

    public Guid AnalysisProcessId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }
    public Guid RequestedByUserId { get; }
    public SourceFileLocation SourceFileLocation { get; }
    public DiagramType DiagramType { get; }
    public DateTime StartedAtUtc { get; }
}
