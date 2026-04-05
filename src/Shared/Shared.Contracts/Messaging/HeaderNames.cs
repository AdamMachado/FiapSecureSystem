namespace Shared.Contracts.Messaging;

public static class HeaderNames
{
    public const string CorrelationId = "x-correlation-id";
    public const string CausationId = "x-causation-id";
    public const string MessageId = "x-message-id";
    public const string MessageType = "x-message-type";
    public const string MessageVersion = "x-message-version";
    public const string Source = "x-source";
    public const string OccurredOnUtc = "x-occurred-on-utc";
}