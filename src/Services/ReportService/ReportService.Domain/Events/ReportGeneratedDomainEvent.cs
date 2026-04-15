using Shared.Kernel.Primitives;
using FiapSecureSystem.ReportService.Domain.ValueObjects;

namespace FiapSecureSystem.ReportService.Domain.Events;

public sealed class ReportGeneratedDomainEvent : DomainEvent
{
    public ReportId ReportId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }

    public ReportGeneratedDomainEvent(
        ReportId reportId,
        AnalysisRequestId analysisRequestId)
    {
        ReportId = reportId;
        AnalysisRequestId = analysisRequestId;
    }
}