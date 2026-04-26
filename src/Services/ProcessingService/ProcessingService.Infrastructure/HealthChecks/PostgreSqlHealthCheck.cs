using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using ProcessingService.Infrastructure.Configuration.Options;

namespace ProcessingService.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly DatabaseOptions _options;

    public PostgreSqlHealthCheck(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is 1 or 1L
                ? HealthCheckResult.Healthy("PostgreSQL is healthy.")
                : HealthCheckResult.Unhealthy("PostgreSQL returned an unexpected result.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.", ex);
        }
    }
}