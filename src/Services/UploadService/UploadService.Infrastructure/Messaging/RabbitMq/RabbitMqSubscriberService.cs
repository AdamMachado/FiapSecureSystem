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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumer in _consumers)
        {
            await ConfigureConsumer(consumer);
        }
    }

    private async Task ConfigureConsumer(RabbitMqConsumerDescriptor descriptor)
    {
        await _channel.Channel.ExchangeDeclareAsync(descriptor.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        await _channel.Channel.QueueDeclareAsync(descriptor.QueueName, durable: true, exclusive: false, autoDelete: false);
        await _channel.Channel.QueueBindAsync(descriptor.QueueName, descriptor.ExchangeName, descriptor.RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
        consumer.ReceivedAsync += _dispatcher.CreateHandler(descriptor);

        await _channel.Channel.BasicConsumeAsync(descriptor.QueueName, autoAck: false, consumer);

        _logger.LogInformation(
            "RabbitMQ consumer configured. Queue: {QueueName}, Exchange: {ExchangeName}, RoutingKey: {RoutingKey}",
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey);
    }
}
