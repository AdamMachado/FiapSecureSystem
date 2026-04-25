using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Kernel.Primitives;

namespace ProcessingService.Application.Abstractions.Messaging;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : DomainEvent
{
    IntegrationEventBase Map(TDomainEvent domainEvent);
}
