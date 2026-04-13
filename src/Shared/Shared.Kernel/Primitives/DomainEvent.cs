namespace Shared.Kernel.Primitives;

public abstract class DomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }
    public DateTime OccurredOnUtc { get; init; }
}