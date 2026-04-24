using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.Events;

public sealed class AnalysisProcessingFailedDomainEvent : DomainEvent
{
    public AnalysisProcessingFailedDomainEvent(
        Guid analysisProcessId,
        AnalysisRequestId analysisRequestId,
        Guid requestedByUserId,
        string reason,
        string? details,
        DateTime failedAtUtc)
    {
        AnalysisProcessId = analysisProcessId;
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        Reason = reason;
        Details = details;
        FailedAtUtc = failedAtUtc;
    }

    public Guid AnalysisProcessId { get; }
    public AnalysisRequestId AnalysisRequestId { get; }
    public Guid RequestedByUserId { get; }
    public string Reason { get; }
    public string? Details { get; }
    public DateTime FailedAtUtc { get; }
}
