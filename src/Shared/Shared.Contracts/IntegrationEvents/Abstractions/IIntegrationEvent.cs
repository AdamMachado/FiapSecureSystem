namespace Shared.Contracts.IntegrationEvents.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    string EventType { get; }
    string EventVersion { get; }
    DateTime OccurredOnUtc { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
}