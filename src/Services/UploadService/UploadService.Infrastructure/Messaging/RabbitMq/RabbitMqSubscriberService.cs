using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.Messaging;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Infrastructure.Configuration.Options;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqSubscriberService : BackgroundService
{
    private readonly RabbitMqChannel _channel;
    private readonly RabbitMqMessageDispatcher _dispatcher;
    private readonly ILogger<RabbitMqSubscriberService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IReadOnlyCollection<RabbitMqConsumerDescriptor> _consumers;

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

        _consumers =
        [
            new RabbitMqConsumerDescriptor(
                QueueNames.AnalysisStarted,
                ExchangeNames.Analysis,
                RoutingKeys.AnalysisStarted,
                ExchangeNames.AnalysisDeadLetter,
                $"{QueueNames.AnalysisStarted}.dead",
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
                ExchangeNames.AnalysisDeadLetter,
                $"{QueueNames.AnalysisCompleted}.dead",
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
                ExchangeNames.AnalysisDeadLetter,
                $"{QueueNames.AnalysisFailed}.dead",
                typeof(AnalysisFailedIntegrationEvent),
                static async (provider, evt, ct) =>
                {
                    var handler = provider.GetRequiredService<IIntegrationEventHandler<AnalysisFailedIntegrationEvent>>();
                    await handler.HandleAsync((AnalysisFailedIntegrationEvent)evt, ct);
                })

            // Adicione apenas se UploadService realmente consumir report.generated:
            //
            // new RabbitMqConsumerDescriptor(
            //     QueueNames.ReportGenerated,
            //     ExchangeNames.Report,
            //     RoutingKeys.ReportGenerated,
            //     typeof(ReportGeneratedIntegrationEvent),
            //     static async (provider, evt, ct) =>
            //     {
            //         var handler = provider.GetRequiredService<IIntegrationEventHandler<ReportGeneratedIntegrationEvent>>();
            //         await handler.HandleAsync((ReportGeneratedIntegrationEvent)evt, ct);
            //     })
        ];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _channel.Channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        foreach (var descriptor in _consumers)
        {
            await ConfigureConsumerAsync(descriptor, stoppingToken);
        }

        _logger.LogInformation(
            "RabbitMQ subscriber started. Consumers: {ConsumerCount}, PrefetchCount: {PrefetchCount}",
            _consumers.Count,
            _options.PrefetchCount);
    }

    private async Task ConfigureConsumerAsync(
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        await EnsureQueueExistsAsync(descriptor, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
        consumer.ReceivedAsync += _dispatcher.CreateHandler(descriptor);

        await _channel.Channel.BasicConsumeAsync(
            queue: descriptor.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer configured. Queue: {QueueName}, Exchange: {ExchangeName}, RoutingKey: {RoutingKey}",
            descriptor.QueueName,
            descriptor.ExchangeName,
            descriptor.RoutingKey);
    }

    private async Task EnsureQueueExistsAsync(
        RabbitMqConsumerDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            await _channel.Channel.QueueDeclarePassiveAsync(
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
}