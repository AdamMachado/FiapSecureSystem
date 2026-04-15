using Shared.Kernel.Primitives;
using FiapSecureSystem.ReportService.Domain.ValueObjects;

namespace FiapSecureSystem.ReportService.Domain.Events;

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