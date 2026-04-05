using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed record AnalysisStartedIntegrationEvent : IntegrationEventBase
{
    public AnalysisStartedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid jobId,
        DateTime startedAtUtc)
        : base(correlationId, causationId)
    {
        JobId = jobId;
        StartedAtUtc = startedAtUtc;
    }

    public override string EventType => nameof(AnalysisStartedIntegrationEvent);

    public Guid JobId { get; init; }
    public DateTime StartedAtUtc { get; init; }
}