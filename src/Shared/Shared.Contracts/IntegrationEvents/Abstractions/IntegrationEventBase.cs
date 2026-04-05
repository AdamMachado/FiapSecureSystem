using Shared.Contracts.Messaging;

namespace Shared.Contracts.IntegrationEvents.Abstractions;

public abstract record IntegrationEventBase : IIntegrationEvent
{
    protected IntegrationEventBase(Guid correlationId, Guid? causationId = null)
    {
        EventId = Guid.NewGuid();
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }
    public abstract string EventType { get; }
    public virtual string EventVersion => MessageVersions.V1;
    public DateTime OccurredOnUtc { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}