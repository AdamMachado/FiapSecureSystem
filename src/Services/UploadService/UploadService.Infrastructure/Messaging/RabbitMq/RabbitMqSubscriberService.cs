using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.Messaging;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqSubscriberService : BackgroundService
{
    private readonly RabbitMqChannel _channel;
    private readonly RabbitMqMessageDispatcher _dispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqSubscriberService> _logger;
    private readonly IReadOnlyCollection<RabbitMqConsumerDescriptor> _consumers;

    public RabbitMqSubscriberService(
        RabbitMqChannel channel,
        RabbitMqMessageDispatcher dispatcher,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqSubscriberService> logger)
    {
        _channel = channel;
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _consumers =
        [
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisStarted,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisStarted,
                typeof(AnalysisStartedIntegrationEvent),
                static async (provider, evt, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisStartedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisStartedIntegrationEvent)evt, ct);
                }),
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisCompleted,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisCompleted,
                typeof(AnalysisCompletedIntegrationEvent),
                static async (provider, evt, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisCompletedIntegrationEvent)evt, ct);
                }),
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisFailed,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisFailed,
                typeof(AnalysisFailedIntegrationEvent),
                static async (provider, evt, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisFailedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisFailedIntegrationEvent)evt, ct);
                })
        ];
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumer in _consumers)
        {
            ConfigureConsumer(consumer);
        }

        return Task.CompletedTask;
    }

    private void ConfigureConsumer(RabbitMqConsumerDescriptor descriptor)
    {
        _channel.Model.ExchangeDeclare(descriptor.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.Model.QueueDeclare(descriptor.QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.Model.QueueBind(descriptor.QueueName, descriptor.ExchangeName, descriptor.RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel.Model);
        consumer.Received += _dispatcher.CreateHandler(descriptor);

        _channel.Model.BasicConsume(descriptor.QueueName, autoAck: false, consumer);

        _logger.LogInformation(
            "RabbitMQ consumer configured. Queue: {QueueName}, Exchange: {ExchangeName}, RoutingKey: {RoutingKey}",
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey);
    }
}
