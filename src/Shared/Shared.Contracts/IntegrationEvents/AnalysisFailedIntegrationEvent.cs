using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed record AnalysisFailedIntegrationEvent : IntegrationEventBase
{
    public AnalysisFailedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid jobId,
        DateTime failedAtUtc,
        string reason,
        string? details = null)
        : base(correlationId, causationId)
    {
        JobId = jobId;
        FailedAtUtc = failedAtUtc;
        Reason = reason;
        Details = details;
    }

    public override string EventType => nameof(AnalysisFailedIntegrationEvent);

    public Guid JobId { get; init; }
    public DateTime FailedAtUtc { get; init; }
    public string Reason { get; init; }
    public string? Details { get; init; }
}