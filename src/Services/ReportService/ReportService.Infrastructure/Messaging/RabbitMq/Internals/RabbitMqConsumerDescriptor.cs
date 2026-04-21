using Shared.Contracts.IntegrationEvents.Abstractions;

namespace ReportService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed record RabbitMqConsumerDescriptor(
    string QueueName,
    string ExchangeName,
    string RoutingKey,
    Type IntegrationEventType,
    Func<IServiceProvider, IntegrationEventBase, CancellationToken, Task> Handler);