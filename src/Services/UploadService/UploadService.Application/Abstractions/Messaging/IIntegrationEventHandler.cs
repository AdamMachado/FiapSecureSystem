using Shared.Contracts.IntegrationEvents.Abstractions;

namespace UploadService.Application.Abstractions.Messaging;

public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IntegrationEventBase
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}