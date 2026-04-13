using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed class AnalysisFailedIntegrationEvent : IntegrationEventBase
{
    public AnalysisFailedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        DateTime failedAtUtc,
        string reason,
        string? details = null)
        : base(correlationId, causationId)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        FailedAtUtc = failedAtUtc;
        Reason = reason;
        Details = details;
    }

    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public DateTime FailedAtUtc { get; init; }
    public string Reason { get; init; }
    public string? Details { get; init; }
}