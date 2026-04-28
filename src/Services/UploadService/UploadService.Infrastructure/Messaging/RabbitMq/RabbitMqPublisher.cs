using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Exceptions;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private readonly RabbitMqChannel _rabbitMqChannel;

    public RabbitMqPublisher(RabbitMqChannel rabbitMqChannel)
    {
        _rabbitMqChannel = rabbitMqChannel;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = _rabbitMqChannel.Channel;
        var routingKey = ResolveRoutingKey(integrationEvent);

        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType()));

            BasicProperties properties = new()
            {
                Persistent = true,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            properties.ApplyIntegrationEventHeaders(
                integrationEvent,
                source: "UploadService");

            await channel.BasicPublishAsync(
                exchange: ResolveExchange(integrationEvent),
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new MessagePublishException(
                $"Failed to publish integration event '{integrationEvent.EventType}' with routing key '{routingKey}'.",
                ex);
        }
    }

    private static string ResolveExchange(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            AnalysisRequestedIntegrationEvent => ExchangeNames.Analysis,
            _ => throw new MessagePublishException($"Unsupported integration event type '{integrationEvent.GetType().Name}'.")
        };
    }

    private static string ResolveRoutingKey(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            AnalysisRequestedIntegrationEvent => RoutingKeys.AnalysisRequested,
            _ => throw new MessagePublishException($"Unsupported integration event type '{integrationEvent.GetType().Name}'.")
        };
    }
}