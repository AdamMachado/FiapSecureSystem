using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Exceptions;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonSerializerOptions();

    private readonly RabbitMqChannel _rabbitMqChannel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ActivitySource _activitySource;

    public RabbitMqPublisher(
        RabbitMqChannel rabbitMqChannel,
        ILogger<RabbitMqPublisher> logger,
        ActivitySource activitySource)
    {
        _rabbitMqChannel = rabbitMqChannel;
        _logger = logger;
        _activitySource = activitySource;

        _rabbitMqChannel.Channel.BasicReturnAsync += OnBasicReturnAsync;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = _rabbitMqChannel.Channel;
        var exchange = ResolveExchange(integrationEvent);
        var routingKey = ResolveRoutingKey(integrationEvent);

        _logger.LogInformation(
            "Publishing integration event to RabbitMQ. EventType: {EventType}, Exchange: {Exchange}, RoutingKey: {RoutingKey}",
            integrationEvent.EventType,
            exchange,
            routingKey);

        using var activity = _activitySource.StartActivity(
            $"RabbitMQ publish {routingKey}",
            ActivityKind.Producer
        );

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.routing_key", routingKey);
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.message.id", integrationEvent.EventId.ToString("N"));
        activity?.SetTag("messaging.message.type", integrationEvent.EventType);
        activity?.SetTag("correlation.id", integrationEvent.CorrelationId.ToString("N"));

        if (integrationEvent.CausationId != null)
            activity?.SetTag("causation.id", integrationEvent.CausationId.Value.ToString("N"));

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
                source: "UploadService");

            properties.InjectTraceContext(activity);

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

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