using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Infrastructure.Configuration.Options;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.Messaging;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqSubscriberService : BackgroundService
{
    private readonly RabbitMqChannel _channel;
    private readonly RabbitMqMessageDispatcher _dispatcher;
    private readonly ILogger<RabbitMqSubscriberService> _logger;
    private readonly RabbitMqOptions _options;

    public RabbitMqSubscriberService(
        RabbitMqChannel channel,
        RabbitMqMessageDispatcher dispatcher,
        ILogger<RabbitMqSubscriberService> logger,
        IOptions<RabbitMqOptions> options)
    {
        _channel = channel;
        _dispatcher = dispatcher;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var descriptor = new RabbitMqConsumerDescriptor(
            QueueNames.AnalysisRequested,
            ExchangeNames.Analysis,
            RoutingKeys.AnalysisRequested,
            typeof(AnalysisRequestedIntegrationEvent),
            static async (sp, message, ct) =>
            {
                var handler = sp.GetRequiredService<IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>>();
                await handler.HandleAsync((AnalysisRequestedIntegrationEvent)message, ct);
            });

        await DeclareConsumerAsync(descriptor, stoppingToken);

        _logger.LogInformation(
            "ProcessingService RabbitMQ subscriber started. Queue={QueueName} RoutingKey={RoutingKey}",
            descriptor.QueueName,
            descriptor.RoutingKey);
    }

    private async Task DeclareConsumerAsync(
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        await _channel.Channel.ExchangeDeclareAsync(
            descriptor.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.Channel.QueueDeclareAsync(
            descriptor.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.Channel.QueueBindAsync(
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.Channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
        consumer.ReceivedAsync += _dispatcher.CreateHandler(descriptor);

        await _channel.Channel.BasicConsumeAsync(
            queue: descriptor.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }
}
