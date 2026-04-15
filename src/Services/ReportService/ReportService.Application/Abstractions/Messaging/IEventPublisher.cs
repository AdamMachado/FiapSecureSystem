using Shared.Contracts.IntegrationEvents.Abstractions;

namespace ReportService.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default);
}