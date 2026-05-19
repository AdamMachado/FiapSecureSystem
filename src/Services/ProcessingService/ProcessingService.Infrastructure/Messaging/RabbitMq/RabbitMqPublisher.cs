using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ProcessingService.Application.Exceptions;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonSerializerOptions();

    private readonly RabbitMqPublisherChannel _publisherChannel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ActivitySource _activitySource;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public RabbitMqPublisher(
        RabbitMqPublisherChannel publisherChannel,
        ILogger<RabbitMqPublisher> logger,
        ActivitySource activitySource)
    {
        _publisherChannel = publisherChannel;
        _logger = logger;
        _activitySource = activitySource;

        _publisherChannel.Channel.BasicReturnAsync += OnBasicReturnAsync;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = _publisherChannel.Channel;
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
                source: "ProcessingService");

            properties.InjectTraceContext(activity);

            await _publishLock.WaitAsync(cancellationToken);

            try
            {
                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _publishLock.Release();
            }

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
            AnalysisExecutionRequestedIntegrationEvent => ExchangeNames.Analysis,
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
            AnalysisExecutionRequestedIntegrationEvent => RoutingKeys.AnalysisExecutionRequested,
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
