using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Messaging;
using System.Text;
using System.Text.Json;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Infrastructure.Exceptions;
using ReportService.Infrastructure.Messaging.RabbitMq.Internals;

namespace ReportService.Infrastructure.Messaging.RabbitMq;

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

            BasicProperties properties = new()
            {
                Persistent = true,
                ContentType = "application/json"
            };

            properties.ApplyIntegrationEventHeaders(
                integrationEvent,
                source: "ReportService");

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
            ReportGeneratedIntegrationEvent => ExchangeNames.Report,
            AnalysisCompletedIntegrationEvent => ExchangeNames.Analysis,
            _ => throw new InvalidOperationException(
                $"No exchange mapped for event type '{integrationEvent.GetType().Name}'.")
        };
    }

    private static string ResolveRoutingKey(IntegrationEventBase integrationEvent)
    {
        return integrationEvent switch
        {
            ReportGeneratedIntegrationEvent => RoutingKeys.ReportGenerated,
            AnalysisCompletedIntegrationEvent => RoutingKeys.AnalysisCompleted,
            _ => throw new InvalidOperationException(
                $"No routing key mapped for event type '{integrationEvent.GetType().Name}'.")
        };
    }
}