namespace ProcessingService.Infrastructure.Configuration.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ClientProvidedName { get; init; } = "processing-service";
    public bool UseSsl { get; init; }
    public ushort PrefetchCount { get; init; } = 5;
    public int MaxRetryCount { get; init; } = 3;
    public int RetryDelayMilliseconds { get; init; } = 30000;
}
