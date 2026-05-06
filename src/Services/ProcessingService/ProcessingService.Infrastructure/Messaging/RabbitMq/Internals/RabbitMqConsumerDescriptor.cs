using Shared.Contracts.IntegrationEvents.Abstractions;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed record RabbitMqConsumerDescriptor(
    string QueueName,
    string ExchangeName,
    string RoutingKey,
    string DeadLetterExchangeName,
    string DeadLetterRoutingKey,
    Type IntegrationEventType,
    Func<IServiceProvider, IntegrationEventBase, CancellationToken, Task> Handler);
