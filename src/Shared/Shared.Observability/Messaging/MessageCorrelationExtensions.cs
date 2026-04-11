using System.Text;
using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Messaging;
using Shared.Observability.Correlation;

namespace Shared.Observability.Messaging;

public static class MessageCorrelationExtensions
{
    public static void ApplyCorrelationHeaders(
        this IBasicProperties properties,
        ICorrelationContextAccessor correlationContextAccessor,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(correlationContextAccessor);

        var correlationId = correlationContextAccessor.CorrelationId ?? Guid.NewGuid().ToString("N");
        var causationId = correlationContextAccessor.CausationId;

        properties.Headers ??= new Dictionary<string, object>();

        SetHeader(properties.Headers, HeaderNames.CorrelationId, correlationId);
        SetHeader(properties.Headers, HeaderNames.CausationId, causationId);
        SetHeader(properties.Headers, HeaderNames.Source, source);

        properties.CorrelationId = correlationId;
        properties.MessageId ??= Guid.NewGuid().ToString("N");
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public static void ApplyIntegrationEventHeaders(
        this IBasicProperties properties,
        IntegrationEventBase integrationEvent,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var context = MessageCorrelationContext.FromIntegrationEvent(integrationEvent, source);

        properties.Headers ??= new Dictionary<string, object>();

        SetHeader(properties.Headers, HeaderNames.CorrelationId, context.CorrelationId);
        SetHeader(properties.Headers, HeaderNames.CausationId, context.CausationId);
        SetHeader(properties.Headers, HeaderNames.MessageId, context.MessageId);
        SetHeader(properties.Headers, HeaderNames.MessageType, context.MessageType);
        SetHeader(properties.Headers, HeaderNames.Source, context.Source);
        SetHeader(properties.Headers, HeaderNames.OccurredOnUtc, context.OccurredOnUtc);

        properties.CorrelationId = context.CorrelationId;
        properties.MessageId = context.MessageId;
        properties.Type = context.MessageType;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public static MessageCorrelationContext ExtractCorrelationContext(this IBasicProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var headers = properties.Headers;

        var correlationId =
            ReadHeader(headers, HeaderNames.CorrelationId) ??
            properties.CorrelationId ??
            Guid.NewGuid().ToString("N");

        var messageId =
            ReadHeader(headers, HeaderNames.MessageId) ??
            properties.MessageId;

        var messageType =
            ReadHeader(headers, HeaderNames.MessageType) ??
            properties.Type;

        return new MessageCorrelationContext
        {
            CorrelationId = correlationId,
            CausationId = ReadHeader(headers, HeaderNames.CausationId),
            MessageId = messageId,
            MessageType = messageType,
            Source = ReadHeader(headers, HeaderNames.Source),
            OccurredOnUtc = ReadHeader(headers, HeaderNames.OccurredOnUtc)
        };
    }

    public static MessageCorrelationContext SetCorrelationContext(
        this IBasicProperties properties,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(correlationContextAccessor);

        var context = properties.ExtractCorrelationContext();

        correlationContextAccessor.CorrelationId = context.CorrelationId;
        correlationContextAccessor.CausationId = context.CausationId;

        return context;
    }

    public static void SetAsCausationFrom(
        this ICorrelationContextAccessor correlationContextAccessor,
        MessageCorrelationContext consumedMessageContext)
    {
        ArgumentNullException.ThrowIfNull(correlationContextAccessor);
        ArgumentNullException.ThrowIfNull(consumedMessageContext);

        correlationContextAccessor.CorrelationId = consumedMessageContext.CorrelationId;
        correlationContextAccessor.CausationId = consumedMessageContext.MessageId;
    }

    public static IDictionary<string, object> CreateHeaders(
        this IntegrationEventBase integrationEvent,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var context = MessageCorrelationContext.FromIntegrationEvent(integrationEvent, source);

        var headers = new Dictionary<string, object>();

        SetHeader(headers, HeaderNames.CorrelationId, context.CorrelationId);
        SetHeader(headers, HeaderNames.CausationId, context.CausationId);
        SetHeader(headers, HeaderNames.MessageId, context.MessageId);
        SetHeader(headers, HeaderNames.MessageType, context.MessageType);
        SetHeader(headers, HeaderNames.Source, context.Source);
        SetHeader(headers, HeaderNames.OccurredOnUtc, context.OccurredOnUtc);

        return headers;
    }

    private static void SetHeader(IDictionary<string, object> headers, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        headers[key] = Encoding.UTF8.GetBytes(value);
    }

    private static string? ReadHeader(IDictionary<string, object>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.ToArray()),
            string str => str,
            _ => value.ToString()
        };
    }
}