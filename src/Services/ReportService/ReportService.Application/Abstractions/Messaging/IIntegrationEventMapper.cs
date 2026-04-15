using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Kernel.Primitives;

namespace ReportService.Application.Abstractions.Messaging;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : DomainEvent
{
    IntegrationEventBase Map(TDomainEvent domainEvent);
}