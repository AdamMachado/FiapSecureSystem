using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Observability.Messaging;

public sealed class MessageCorrelationContext
{
    public string CorrelationId { get; init; } = default!;
    public string? CausationId { get; init; }
    public string? MessageId { get; init; }
    public string? MessageType { get; init; }
    public string? MessageVersion { get; init; }
    public string? Source { get; init; }
    public string? OccurredOnUtc { get; init; }

    public static MessageCorrelationContext FromIntegrationEvent(IIntegrationEvent integrationEvent, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new MessageCorrelationContext
        {
            CorrelationId = integrationEvent.CorrelationId.ToString("N"),
            CausationId = integrationEvent.CausationId?.ToString("N"),
            MessageId = integrationEvent.EventId.ToString("N"),
            MessageType = integrationEvent.EventType,
            MessageVersion = integrationEvent.EventVersion,
            Source = source,
            OccurredOnUtc = integrationEvent.OccurredOnUtc.ToString("O")
        };
    }

    public static MessageCorrelationContext CreateNew(string? source = null)
    {
        return new MessageCorrelationContext
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            Source = source
        };
    }
}