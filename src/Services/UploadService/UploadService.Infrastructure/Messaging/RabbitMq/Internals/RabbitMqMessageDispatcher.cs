using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;
using Shared.Observability.Messaging;

namespace UploadService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqMessageDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqMessageDispatcher> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public RabbitMqMessageDispatcher(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqMessageDispatcher> logger,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public AsyncEventHandler<BasicDeliverEventArgs> CreateHandler(RabbitMqConsumerDescriptor descriptor)
    {
        return async (_, args) =>
        {
            using var scope = _serviceProvider.CreateScope();

            var model = scope.ServiceProvider.GetRequiredService<RabbitMqChannel>().Model;
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
                    throw new InvalidOperationException(
                        $"Could not deserialize message to '{descriptor.IntegrationEventType.Name}'.");

                await descriptor.Handler(scope.ServiceProvider, integrationEvent, CancellationToken.None);
                model.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to handle RabbitMQ message. Queue: {QueueName}, RoutingKey: {RoutingKey}",
                    descriptor.QueueName,
                    args.RoutingKey);

                model.BasicNack(args.DeliveryTag, false, false);
            }
            finally
            {
                _correlationContextAccessor.Clear();
            }
        };
    }
}
