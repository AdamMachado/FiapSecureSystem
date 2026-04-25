using Shared.Contracts.IntegrationEvents.Abstractions;

namespace ProcessingService.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default);
}
