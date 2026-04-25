using Shared.Contracts.IntegrationEvents.Abstractions;

namespace ProcessingService.Application.Abstractions.Messaging;

public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IntegrationEventBase
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
