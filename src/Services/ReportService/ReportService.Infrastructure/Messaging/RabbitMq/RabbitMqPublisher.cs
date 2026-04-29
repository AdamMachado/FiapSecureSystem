using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Exceptions;
using ReportService.Infrastructure.Messaging.RabbitMq.Internals;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReportService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonSerializerOptions();

    private readonly RabbitMqChannel _rabbitMqChannel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        RabbitMqChannel rabbitMqChannel,
        ILogger<RabbitMqPublisher> logger)
    {
        _rabbitMqChannel = rabbitMqChannel;
        _logger = logger;

        _rabbitMqChannel.Channel.BasicReturnAsync += OnBasicReturnAsync;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = _rabbitMqChannel.Channel;
        var exchange = ResolveExchange(integrationEvent);
        var routingKey = ResolveRoutingKey(integrationEvent);

        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType(),
                JsonOptions));

            BasicProperties properties = new()
            {
                Persistent = true,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            properties.ApplyIntegrationEventHeaders(
                integrationEvent,
                source: "ReportService");

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
                $"Failed to publish integration event '{integrationEvent.EventType}' " +
                $"to exchange '{exchange}' with routing key '{routingKey}'.",
                ex);
        }
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string ResolveExchange(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            ReportGeneratedIntegrationEvent => ExchangeNames.Report,
            _ => throw new InvalidOperationException(
                $"No exchange mapped for event type '{integrationEvent.GetType().Name}'.")
        };
    }

    private static string ResolveRoutingKey(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            ReportGeneratedIntegrationEvent => RoutingKeys.ReportGenerated,
            _ => throw new InvalidOperationException(
                $"No routing key mapped for event type '{integrationEvent.GetType().Name}'.")
        };
    }

    private Task OnBasicReturnAsync(object sender, BasicReturnEventArgs args)
    {
        _logger.LogError(
            "RabbitMQ returned an unroutable message. Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
            args.Exchange,
            args.RoutingKey,
            args.ReplyCode,
            args.ReplyText);

        return Task.CompletedTask;
    }
}