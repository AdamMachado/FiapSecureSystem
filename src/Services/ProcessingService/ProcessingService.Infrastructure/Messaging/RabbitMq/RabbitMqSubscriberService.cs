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
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqMessageDispatcher _dispatcher;
    private readonly ILogger<RabbitMqSubscriberService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IReadOnlyCollection<RabbitMqConsumerDescriptor> _consumers;
    private readonly List<RabbitMqConsumerChannel> _consumerChannels = [];

    public RabbitMqSubscriberService(
        RabbitMqConnection connection,
        RabbitMqMessageDispatcher dispatcher,
        ILogger<RabbitMqSubscriberService> logger,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _dispatcher = dispatcher;
        _logger = logger;
        _options = options.Value;

        var requestedPrefetchCount = NormalizePrefetchCount(_options.PrefetchCount);
        var executionConcurrency = NormalizePrefetchCount(_options.AnalysisExecutionMaxConcurrency);
        var executionPrefetchCount = NormalizePrefetchCount((ushort)Math.Max(requestedPrefetchCount, executionConcurrency));

        _consumers =
        [
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisRequested,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisRequested,
                ExchangeNames.AnalysisDeadLetter,
                $"{QueueNames.AnalysisRequested}.dead",
                typeof(AnalysisRequestedIntegrationEvent),
                requestedPrefetchCount,
                1,
                static async (provider, message, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisRequestedIntegrationEvent)message, ct);
                }),
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisExecutionRequested,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisExecutionRequested,
                ExchangeNames.AnalysisDeadLetter,
                $"{QueueNames.AnalysisExecutionRequested}.dead",
                typeof(AnalysisExecutionRequestedIntegrationEvent),
                executionPrefetchCount,
                executionConcurrency,
                static async (provider, message, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisExecutionRequestedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisExecutionRequestedIntegrationEvent)message, ct);
                })
        ];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            foreach (var descriptor in _consumers)
            {
                await ConfigureConsumerAsync(descriptor, stoppingToken);
            }

            _logger.LogInformation(
                "RabbitMQ subscriber started. Consumers: {ConsumerCount}, RequestedPrefetchCount: {RequestedPrefetchCount}, AnalysisExecutionMaxConcurrency: {AnalysisExecutionMaxConcurrency}",
                _consumers.Count,
                NormalizePrefetchCount(_options.PrefetchCount),
                NormalizePrefetchCount(_options.AnalysisExecutionMaxConcurrency));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            foreach (var consumerChannel in _consumerChannels)
            {
                await consumerChannel.DisposeAsync();
            }
        }
    }

    private async Task ConfigureConsumerAsync(
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var consumerChannel = await RabbitMqConsumerChannel.CreateAsync(
            _connection.Connection,
            descriptor.ConsumerDispatchConcurrency,
            cancellationToken);

        _consumerChannels.Add(consumerChannel);

        await consumerChannel.Channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: descriptor.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        await EnsureQueueExistsAsync(consumerChannel.Channel, descriptor, cancellationToken);

        var consumerChannelLock = new SemaphoreSlim(1, 1);
        var consumer = new AsyncEventingBasicConsumer(consumerChannel.Channel);
        consumer.ReceivedAsync += _dispatcher.CreateHandler(
            consumerChannel.Channel,
            consumerChannelLock,
            descriptor);

        await consumerChannel.Channel.BasicConsumeAsync(
            queue: descriptor.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer configured. Queue: {QueueName}, Exchange: {ExchangeName}, RoutingKey: {RoutingKey}, PrefetchCount: {PrefetchCount}, ConsumerDispatchConcurrency: {ConsumerDispatchConcurrency}",
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey,
            descriptor.PrefetchCount,
            descriptor.ConsumerDispatchConcurrency);
    }

    private async Task EnsureQueueExistsAsync(
        IChannel channel,
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            await channel.QueueDeclarePassiveAsync(
                queue: descriptor.QueueName,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "RabbitMQ queue '{QueueName}' does not exist or is not accessible. " +
                "Topology must be created by init-rabbitmq.sh before starting this service. " +
                "Expected exchange: {ExchangeName}, routing key: {RoutingKey}",
                descriptor.QueueName,
                descriptor.ExchangeName,
                descriptor.RoutingKey);

            throw;
        }
    }

    private static ushort NormalizePrefetchCount(ushort value)
    {
        return value == 0 ? (ushort)1 : value;
    }
}
