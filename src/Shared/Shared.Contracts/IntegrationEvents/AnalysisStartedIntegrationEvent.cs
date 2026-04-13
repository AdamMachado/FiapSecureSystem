using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed class AnalysisStartedIntegrationEvent : IntegrationEventBase
{
    public AnalysisStartedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        DateTime startedAtUtc)
        : base(correlationId, causationId)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        StartedAtUtc = startedAtUtc;
    }

    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public DateTime StartedAtUtc { get; init; }
}