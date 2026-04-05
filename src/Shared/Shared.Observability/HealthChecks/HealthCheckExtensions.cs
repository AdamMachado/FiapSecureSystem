using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Observability.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddSharedHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is healthy"));

        return services;
    }

    public static IApplicationBuilder UseSharedHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        return app;
    }
}