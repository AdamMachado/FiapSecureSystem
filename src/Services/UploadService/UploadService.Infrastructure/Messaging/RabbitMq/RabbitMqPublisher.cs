using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Infrastructure.Exceptions;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;

namespace UploadService.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IEventPublisher
{
    private readonly RabbitMqChannel _rabbitMqChannel;

    public RabbitMqPublisher(RabbitMqChannel rabbitMqChannel)
    {
        _rabbitMqChannel = rabbitMqChannel;
    }

    public async Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = _rabbitMqChannel.Channel;

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType()));

            BasicProperties properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            properties.ApplyIntegrationEventHeaders(
                integrationEvent,
                source: "UploadService");

            await channel.BasicPublishAsync(
                exchange: ResolveExchange(integrationEvent),
                routingKey: ResolveRoutingKey(integrationEvent),
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new MessagePublishException("Failed to publish RabbitMQ message.", ex);
        }
    }

    private static string ResolveExchange(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            _ => Shared.Contracts.Messaging.ExchangeNames.Analysis
        };
    }

    private static string ResolveRoutingKey(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            Shared.Contracts.IntegrationEvents.AnalysisRequestedIntegrationEvent
                => Shared.Contracts.Messaging.RoutingKeys.AnalysisRequested,

            Shared.Contracts.IntegrationEvents.AnalysisStartedIntegrationEvent
                => Shared.Contracts.Messaging.RoutingKeys.AnalysisStarted,

            Shared.Contracts.IntegrationEvents.AnalysisCompletedIntegrationEvent
                => Shared.Contracts.Messaging.RoutingKeys.AnalysisCompleted,

            Shared.Contracts.IntegrationEvents.AnalysisFailedIntegrationEvent
                => Shared.Contracts.Messaging.RoutingKeys.AnalysisFailed,

            Shared.Contracts.IntegrationEvents.ReportGeneratedIntegrationEvent
                => Shared.Contracts.Messaging.RoutingKeys.ReportGenerated,

            _ => throw new InvalidOperationException(
                $"No routing key mapped for event type '{integrationEvent.GetType().Name}'.")
        };
    }
}