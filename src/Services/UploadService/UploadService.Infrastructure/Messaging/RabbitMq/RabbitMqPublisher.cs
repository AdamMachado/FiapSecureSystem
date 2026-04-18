using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Infrastructure.Exceptions;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        RabbitMqChannel channel,
        ILogger<RabbitMqPublisher> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        try
        {
            var (exchange, routingKey) = ResolveRoute(integrationEvent);

            _channel.Model.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            var properties = _channel.Model.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = integrationEvent.EventType;
            properties.ApplyIntegrationEventHeaders(integrationEvent, source: "SOAT.UploadService");

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions));

            _channel.Model.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published integration event {EventType} to exchange {Exchange} with routing key {RoutingKey}.",
                integrationEvent.EventType,
                exchange,
                routingKey);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new MessagePublishException(
                $"Failed to publish integration event '{integrationEvent.EventType}'.",
                ex);
        }
    }

    private static (string Exchange, string RoutingKey) ResolveRoute(IntegrationEventBase integrationEvent)
        => integrationEvent switch
        {
            AnalysisRequestedIntegrationEvent => (ExchangeNames.Analysis, RoutingKeys.AnalysisRequested),
            _ => throw new InvalidOperationException(
                $"There is no route configured for event type '{integrationEvent.GetType().Name}'.")
        };
}
