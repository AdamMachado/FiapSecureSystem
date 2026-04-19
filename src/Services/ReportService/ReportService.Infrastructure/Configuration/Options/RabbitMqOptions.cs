namespace ReportService.Infrastructure.Configuration.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ClientProvidedName { get; init; } = "report-service";
    public bool UseSsl { get; init; }
    public ushort PrefetchCount { get; init; } = 10;
}