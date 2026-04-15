using Shared.Kernel.Primitives;
using FiapSecureSystem.ReportService.Domain.ValueObjects;

namespace FiapSecureSystem.ReportService.Domain.Events;

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