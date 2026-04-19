using Shared.Kernel.Primitives;
using ReportService.Domain.ValueObjects;

namespace ReportService.Domain.Events;

public sealed class ReportGenerationRequestedDomainEvent : DomainEvent
{
    public ReportId ReportId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }

    public ReportGenerationRequestedDomainEvent(
        ReportId reportId,
        AnalysisRequestId analysisRequestId)
    {
        ReportId = reportId;
        AnalysisRequestId = analysisRequestId;
    }
}