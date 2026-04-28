using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportService.Infrastructure.Configuration.Options;
using ReportService.Infrastructure.Messaging.RabbitMq.Internals;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;

namespace ReportService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqMessageDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqMessageDispatcher> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly RabbitMqOptions _rabbitMqOptions;

    public RabbitMqMessageDispatcher(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqMessageDispatcher> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    public AsyncEventHandler<BasicDeliverEventArgs> CreateHandler(RabbitMqConsumerDescriptor descriptor)
    {
        return async (_, args) =>
        {
            using var scope = _serviceProvider.CreateScope();

            var channel = scope.ServiceProvider.GetRequiredService<RabbitMqChannel>().Channel;
            var correlation = args.BasicProperties.ExtractCorrelationContext();

            _correlationContextAccessor.CorrelationId = correlation.CorrelationId;
            _correlationContextAccessor.CausationId = correlation.MessageId;

            try
            {
                var payload = JsonSerializer.Deserialize(
                    args.Body.Span,
                    descriptor.IntegrationEventType,
                    JsonOptions);

                if (payload is not IntegrationEventBase integrationEvent)
                {
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
            }
            catch (Exception ex)
            {
                var deathCount = GetDeathCount(args.BasicProperties, descriptor.QueueName);
                var maxRetryCount = Math.Max(0, _rabbitMqOptions.MaxRetryCount);

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

                    return;
                }

                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
            finally
            {
                _correlationContextAccessor.Clear();
            }
        };
    }

    private async Task PublishToDeadLetterQueueAsync(
        IChannel channel,
        RabbitMqConsumerDescriptor descriptor,
        BasicDeliverEventArgs args,
        Exception exception)
    {
        _logger.LogWarning(
            exception,
            "RabbitMQ message exceeded retry limit and will be published to DLQ. Queue: {QueueName}, DeadLetterExchange: {DeadLetterExchange}, DeadLetterRoutingKey: {DeadLetterRoutingKey}, MessageType: {MessageType}",
            descriptor.QueueName,
            descriptor.DeadLetterExchangeName,
            descriptor.DeadLetterRoutingKey,
            descriptor.IntegrationEventType.Name);

        BasicProperties properties = new BasicProperties(args.BasicProperties);

        await channel.BasicPublishAsync(
            exchange: descriptor.DeadLetterExchangeName,
            routingKey: descriptor.DeadLetterRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: args.Body);
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