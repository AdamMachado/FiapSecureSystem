using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.Messaging;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Infrastructure.Messaging.RabbitMq.Internals;

namespace ReportService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqSubscriberService : BackgroundService
{
    private readonly RabbitMqChannel _channel;
    private readonly RabbitMqMessageDispatcher _dispatcher;
    private readonly ILogger<RabbitMqSubscriberService> _logger;
    private readonly IReadOnlyCollection<RabbitMqConsumerDescriptor> _consumers;

    public RabbitMqSubscriberService(
        RabbitMqChannel channel,
        RabbitMqMessageDispatcher dispatcher,
        ILogger<RabbitMqSubscriberService> logger)
    {
        _channel = channel;
        _dispatcher = dispatcher;
        _logger = logger;

        _consumers =
        [
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisCompleted,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisCompleted,
                typeof(AnalysisCompletedIntegrationEvent),
                static async (provider, evt, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisCompletedIntegrationEvent)evt, ct);
                })
        ];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumer in _consumers)
        {
            await ConfigureConsumerAsync(consumer, stoppingToken);
        }
    }

    private async Task ConfigureConsumerAsync(
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        await _channel.Channel.ExchangeDeclareAsync(
            descriptor.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.Channel.QueueDeclareAsync(
            descriptor.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.Channel.QueueBindAsync(
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
        consumer.ReceivedAsync += _dispatcher.CreateHandler(descriptor);

        await _channel.Channel.BasicConsumeAsync(
            descriptor.QueueName,
            autoAck: false,
            consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer configured. Queue: {QueueName}, Exchange: {ExchangeName}, RoutingKey: {RoutingKey}",
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey);
    }
}