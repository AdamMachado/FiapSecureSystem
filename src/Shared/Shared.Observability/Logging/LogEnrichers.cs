using Serilog.Core;
using Serilog.Events;
using Shared.Observability.Correlation;

namespace Shared.Observability.Logging;

public sealed class CorrelationLogEnricher : ILogEventEnricher
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationLogEnricher(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _correlationContextAccessor.CorrelationId;
        var causationId = _correlationContextAccessor.CausationId;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("CorrelationId", correlationId));
        }

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("CausationId", causationId));
        }
    }
}