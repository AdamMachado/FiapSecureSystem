using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportService.Application.Exceptions;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

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
        var exchange = ResolveExchangeName(integrationEvent);
        var routingKey = ResolveRoutingKey(integrationEvent);

        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType(),
                JsonSerializerOptions));

            BasicProperties properties = new()
            {
                Persistent = true,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            properties.ApplyIntegrationEventHeaders(
                integrationEvent,
                source: "ProcessingService");

            await channel.BasicPublishAsync(
                exchange: exchange,
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
