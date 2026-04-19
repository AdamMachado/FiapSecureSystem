using Shared.Kernel.Primitives;
using ReportService.Domain.ValueObjects;

namespace ReportService.Domain.Events;

public sealed class ReportGenerationFailedDomainEvent : DomainEvent
{
    public ReportId ReportId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }
    public string Reason { get; }

    public ReportGenerationFailedDomainEvent(
        ReportId reportId,
        AnalysisRequestId analysisRequestId,
        string reason)
    {
        ReportId = reportId;
        AnalysisRequestId = analysisRequestId;
        Reason = reason;
    }
}