using Shared.Kernel.Primitives;
using UploadService.Domain.Enums;

namespace UploadService.Domain.Events;

public class AnalysisStatusChangedDomainEvent : DomainEvent
{
    public Guid AnalysisRequestId { get; }
    public AnalysisStatus PreviousStatus { get; }
    public AnalysisStatus CurrentStatus { get; }
    public string? FailureReason { get; }

    public AnalysisStatusChangedDomainEvent(
        Guid analysisRequestId,
        AnalysisStatus previousStatus,
        AnalysisStatus currentStatus,
        string? failureReason = null)
    {
        AnalysisRequestId = analysisRequestId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        FailureReason = failureReason;
    }
}