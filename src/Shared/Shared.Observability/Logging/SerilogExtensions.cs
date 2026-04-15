using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Observability.Correlation;

namespace Shared.Observability.Logging;

public static class SerilogExtensions
{
    public static IServiceCollection AddSharedSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName)
    {
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddSingleton<CorrelationLogEnricher>();

        services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.With(services.GetRequiredService<CorrelationLogEnricher>());
        });

        return services;
    }
}