using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProcessingService.Infrastructure.HealthChecks;

public static class PostgreSqlHealthChecks
{
    public static IHealthChecksBuilder AddPostgreSqlHealthChecks(
        this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<PostgreSqlHealthCheck>(
            name: "postgresql",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["database", "postgresql"]);
    }
}