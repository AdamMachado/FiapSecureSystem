using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.Username,
                Password = _options.Password,
                ClientProvidedName = $"{_options.ClientProvidedName}.healthcheck"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is reachable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", ex));
        }
    }
}
