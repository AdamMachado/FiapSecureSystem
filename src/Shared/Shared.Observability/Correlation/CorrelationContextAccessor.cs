using System.Collections.Concurrent;

namespace Shared.Observability.Correlation;

public sealed class CorrelationContextAccessor
{
    private const string CorrelationIdKey = "CorrelationId";
    private const string CausationIdKey = "CausationId";

    private static readonly AsyncLocal<ConcurrentDictionary<string, object?>> Context = new();

    public string? CorrelationId
    {
        get => Get<string>(CorrelationIdKey);
        set => Set(CorrelationIdKey, value);
    }

    public string? CausationId
    {
        get => Get<string>(CausationIdKey);
        set => Set(CausationIdKey, value);
    }

    public Guid GetOrCreateCorrelationGuid()
    {
        if (Guid.TryParse(CorrelationId, out var correlationId))
            return correlationId;

        correlationId = Guid.NewGuid();
        CorrelationId = correlationId.ToString("N");
        return correlationId;
    }

    public Guid? GetCausationGuidOrNull()
    {
        return Guid.TryParse(CausationId, out var causationId)
            ? causationId
            : null;
    }

    public void Clear()
    {
        Context.Value?.Clear();
    }

    private static T? Get<T>(string key)
    {
        if (Context.Value is null)
            return default;

        if (!Context.Value.TryGetValue(key, out var value))
            return default;

        return value is T typed ? typed : default;
    }

    private static void Set(string key, object? value)
    {
        Context.Value ??= new ConcurrentDictionary<string, object?>();
        Context.Value[key] = value;
    }
}