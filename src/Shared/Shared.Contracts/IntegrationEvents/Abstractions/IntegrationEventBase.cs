using Shared.Contracts.Messaging;

namespace Shared.Contracts.IntegrationEvents.Abstractions;

public abstract class IntegrationEventBase
{
    protected IntegrationEventBase(Guid correlationId, Guid? causationId = null, string source = null, DateTime? occurredOnUtc = null)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId cannot be empty.", nameof(correlationId));

        EventId = Guid.NewGuid();
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredOnUtc = occurredOnUtc ?? DateTime.UtcNow;
        Source = source ?? string.Empty;
    }

    public Guid EventId { get; init; }
    public string Source { get; init; }
    public virtual string EventType => GetType().Name;
    public DateTime OccurredOnUtc { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}