using Shared.Contracts.IntegrationEvents.Abstractions;

namespace UploadService.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default);
}