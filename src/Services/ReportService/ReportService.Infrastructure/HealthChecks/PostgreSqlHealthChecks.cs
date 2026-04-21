using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReportService.Infrastructure.Configuration.Options;

namespace ReportService.Infrastructure.HealthChecks;

public static class PostgreSqlHealthChecks
{
    public static IHealthChecksBuilder AddPostgreSqlHealthChecks(
        this IHealthChecksBuilder builder,
        DatabaseOptions options)
    {
        builder.AddNpgSql(
            options.ConnectionString,
            name: "postgresql",
            failureStatus: HealthStatus.Unhealthy);

        return builder;
    }
}