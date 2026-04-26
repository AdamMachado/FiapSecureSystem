using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Infrastructure.Exceptions;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private readonly RabbitMqChannel _rabbitMqChannel;

    public RabbitMqPublisher(RabbitMqChannel rabbitMqChannel)
    {
        _rabbitMqChannel = rabbitMqChannel;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var exchangeName = ResolveExchangeName(integrationEvent);
        var routingKey = ResolveRoutingKey(integrationEvent);
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), JsonSerializerOptions);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = integrationEvent.EventId.ToString(),
            Type = integrationEvent.EventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = integrationEvent.CreateHeaders("ProcessingService")
        };

        try
        {
            await _rabbitMqChannel.Channel.BasicPublishAsync(
                exchange: exchangeName,
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

    private static string ResolveExchangeName(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            AnalysisStartedIntegrationEvent => ExchangeNames.Analysis,
            AnalysisCompletedIntegrationEvent => ExchangeNames.Analysis,
            AnalysisFailedIntegrationEvent => ExchangeNames.Analysis,
            _ => throw new MessagePublishException($"Unsupported integration event type '{integrationEvent.GetType().Name}'.")
        };
    }

    private static string ResolveRoutingKey(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            AnalysisStartedIntegrationEvent => RoutingKeys.AnalysisStarted,
            AnalysisCompletedIntegrationEvent => RoutingKeys.AnalysisCompleted,
            AnalysisFailedIntegrationEvent => RoutingKeys.AnalysisFailed,
            _ => throw new MessagePublishException($"Unsupported integration event type '{integrationEvent.GetType().Name}'.")
        };
    }
}
