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
        services.AddSingleton<CorrelationLogEnricher>();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .CreateLogger();

        return services;
    }

    public static LoggerConfiguration AddSharedEnrichers(
        this LoggerConfiguration loggerConfiguration,
        IServiceProvider serviceProvider,
        string applicationName)
    {
        var correlationAccessor = serviceProvider.GetRequiredService<CorrelationContextAccessor>();

        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.With(new CorrelationLogEnricher(correlationAccessor));
    }
}