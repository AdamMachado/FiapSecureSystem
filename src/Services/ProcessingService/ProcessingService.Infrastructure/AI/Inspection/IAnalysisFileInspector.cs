using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;

namespace ProcessingService.Infrastructure.AI.Inspection;

public interface IAnalysisFileInspector
{
    bool CanInspect(DiagramType diagramType);

    Task<AnalysisFileInspectionResult> InspectAsync(
        ArchitectureAnalysisRequest request,
        byte[] content,
        CancellationToken cancellationToken = default);
}
