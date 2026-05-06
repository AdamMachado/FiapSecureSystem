using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace Shared.Observability.Messaging;

public static class MessageTraceContextExtensions
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void InjectTraceContext(
        this IBasicProperties properties,
        Activity? activity)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (activity is null)
            return;

        properties.Headers ??= new Dictionary<string, object?>();

        var propagationContext = new PropagationContext(
            activity.Context,
            Baggage.Current);

        Propagator.Inject(
            propagationContext,
            properties.Headers,
            static (headers, key, value) =>
            {
                headers[key] = Encoding.UTF8.GetBytes(value);
            });
    }

    public static PropagationContext ExtractTraceContext(
        this IReadOnlyBasicProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return Propagator.Extract(
            default,
            properties.Headers,
            static (headers, key) =>
            {
                if (headers is null ||
                    !headers.TryGetValue(key, out var value) ||
                    value is null)
                {
                    return [];
                }

                return value switch
                {
                    byte[] bytes => [Encoding.UTF8.GetString(bytes)],
                    ReadOnlyMemory<byte> memory => [Encoding.UTF8.GetString(memory.Span)],
                    string text => [text],
                    _ => [value.ToString() ?? string.Empty]
                };
            });
    }
}