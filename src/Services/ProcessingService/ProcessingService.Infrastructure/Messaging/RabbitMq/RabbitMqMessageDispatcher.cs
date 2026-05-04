using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using ProcessingService.Infrastructure.Configuration.Options;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;
using Shared.Observability.Messaging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqMessageDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonSerializerOptions();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqMessageDispatcher> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ActivitySource _activitySource;

    public RabbitMqMessageDispatcher(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqMessageDispatcher> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ActivitySource activitySource)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _activitySource = activitySource;
    }

    public AsyncEventHandler<BasicDeliverEventArgs> CreateHandler(RabbitMqConsumerDescriptor descriptor)
    {
        _logger.LogInformation(
            "Creating RabbitMQ message handler. Queue: {QueueName}, RoutingKey: {RoutingKey}, MessageType: {MessageType}",
            descriptor.QueueName,
            descriptor.RoutingKey,
            descriptor.IntegrationEventType.Name);

        return async (_, args) =>
        {
            using var scope = _serviceProvider.CreateScope();

            var channel = scope.ServiceProvider.GetRequiredService<RabbitMqChannel>().Channel;
            var propagationContext = args.BasicProperties.ExtractTraceContext();
            var previousBaggage = Baggage.Current;
            Baggage.Current = propagationContext.Baggage;

            using var activity = _activitySource.StartActivity(
                $"RabbitMQ consume {descriptor.RoutingKey}",
                ActivityKind.Consumer,
                propagationContext.ActivityContext);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", descriptor.ExchangeName);
            activity?.SetTag("messaging.rabbitmq.routing_key", args.RoutingKey);
            activity?.SetTag("messaging.operation", "consume");
            activity?.SetTag("messaging.consumer.queue", descriptor.QueueName);
            activity?.SetTag("messaging.message.type", descriptor.IntegrationEventType.Name);
            var correlation = args.BasicProperties.ExtractCorrelationContext();

            _correlationContextAccessor.CorrelationId = correlation.CorrelationId;
            _correlationContextAccessor.CausationId = correlation.MessageId ?? Guid.NewGuid().ToString("N");

            activity?.SetTag("correlation.id", correlation.CorrelationId);

            if (!string.IsNullOrWhiteSpace(correlation.MessageId))
            {
                activity?.SetTag("causation.id", correlation.MessageId);
                activity?.SetTag("messaging.message.id", correlation.MessageId);
            }

            try
            {
                var payload = JsonSerializer.Deserialize(
                    args.Body.Span,
                    descriptor.IntegrationEventType,
                    JsonOptions);

                if (payload is not IntegrationEventBase integrationEvent)
                {
                    _logger.LogError(
                        "Failed to deserialize RabbitMQ message. Queue: {QueueName}, RoutingKey: {RoutingKey}, Type: {MessageType}",
                        descriptor.QueueName,
                        args.RoutingKey,
                        descriptor.IntegrationEventType.Name);

                    throw new InvalidOperationException(
                        $"Could not deserialize message to '{descriptor.IntegrationEventType.Name}'.");
                }

                await descriptor.Handler(
                    scope.ServiceProvider,
                    integrationEvent,
                    CancellationToken.None);

                await channel.BasicAckAsync(
                    args.DeliveryTag,
                    multiple: false);

                _logger.LogInformation(
                    "Successfully handled RabbitMQ message. Queue: {QueueName}, RoutingKey: {RoutingKey}, Type: {MessageType}",
                    descriptor.QueueName,
                    args.RoutingKey,
                    descriptor.IntegrationEventType.Name);

                activity?.SetTag("messaging.rabbitmq.ack", true);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                activity?.SetTag("exception.message", ex.Message);

                var deathCount = GetDeathCount(args.BasicProperties, descriptor.QueueName);
                var maxRetryCount = Math.Max(0, _rabbitMqOptions.MaxRetryCount);

                activity?.SetTag("messaging.rabbitmq.death_count", deathCount);
                activity?.SetTag("messaging.rabbitmq.max_retry_count", maxRetryCount);

                _logger.LogError(
                    ex,
                    "Failed to handle RabbitMQ message. Queue: {QueueName}, RoutingKey: {RoutingKey}, Type: {MessageType}, DeathCount: {DeathCount}, MaxRetryCount: {MaxRetryCount}",
                    descriptor.QueueName,
                    args.RoutingKey,
                    descriptor.IntegrationEventType.Name,
                    deathCount,
                    maxRetryCount);

                if (deathCount >= maxRetryCount)
                {
                    await PublishToDeadLetterQueueAsync(
                        channel,
                        descriptor,
                        args,
                        ex);

                    await channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);

                    activity?.SetTag("messaging.rabbitmq.dead_lettered", true);
                    activity?.SetTag("messaging.rabbitmq.ack", true);
                    return;
                }

                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false);

                _logger.LogWarning(
                    "RabbitMQ message has been negatively acknowledged and will be requeued by RabbitMQ. Queue: {QueueName}, RoutingKey: {RoutingKey}, Type: {MessageType}",
                    descriptor.QueueName,
                    args.RoutingKey,
                    descriptor.IntegrationEventType.Name);

                activity?.SetTag("messaging.rabbitmq.nack", true);
            }
            finally
            {
                Baggage.Current = previousBaggage;
                _correlationContextAccessor.Clear();
            }
        };
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private async Task PublishToDeadLetterQueueAsync(
        IChannel channel,
        RabbitMqConsumerDescriptor descriptor,
        BasicDeliverEventArgs args,
        Exception exception)
    {
        using var activity = _activitySource.StartActivity(
            $"RabbitMQ publish to DLQ {descriptor.DeadLetterRoutingKey}",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", descriptor.DeadLetterExchangeName);
        activity?.SetTag("messaging.rabbitmq.routing_key", descriptor.DeadLetterRoutingKey);
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.rabbitmq.dead_letter", true);
        activity?.SetTag("exception.type", exception.GetType().FullName);
        activity?.SetTag("exception.message", exception.Message);

        _logger.LogWarning(
            exception,
            "RabbitMQ message exceeded retry limit and will be published to DLQ. Queue: {QueueName}, DeadLetterExchange: {DeadLetterExchange}, DeadLetterRoutingKey: {DeadLetterRoutingKey}, MessageType: {MessageType}",
            descriptor.QueueName,
            descriptor.DeadLetterExchangeName,
            descriptor.DeadLetterRoutingKey,
            descriptor.IntegrationEventType.Name);

        BasicProperties properties = new BasicProperties(args.BasicProperties);
        properties.InjectTraceContext(activity);

        await channel.BasicPublishAsync(
            exchange: descriptor.DeadLetterExchangeName,
            routingKey: descriptor.DeadLetterRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: args.Body);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static long GetDeathCount(IReadOnlyBasicProperties properties, string queueName)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue("x-death", out var xDeathObj) ||
            xDeathObj is not IList<object> deaths)
        {
            return 0;
        }

        foreach (var death in deaths)
        {
            if (death is not IDictionary<string, object> deathDict)
                continue;

            var deadQueueName = TryGetHeaderString(deathDict, "queue");

            if (!string.Equals(deadQueueName, queueName, StringComparison.Ordinal))
                continue;

            if (!deathDict.TryGetValue("count", out var count))
                return 0;

            return count switch
            {
                long value => value,
                int value => value,
                uint value => value,
                ulong value => checked((long)value),
                short value => value,
                byte value => value,
                _ => Convert.ToInt64(count)
            };
        }

        return 0;
    }

    private static string? TryGetHeaderString(
        IDictionary<string, object> headers,
        string key)
    {
        if (!headers.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            null => null,
            string text => text,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => value.ToString()
        };
    }
}