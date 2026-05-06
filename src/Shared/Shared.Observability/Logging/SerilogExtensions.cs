using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.TryAddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.TryAddSingleton<CorrelationLogEnricher>();

        services.AddSerilog((services, loggerConfiguration) =>
        {
            var otlpEndpoint =
                configuration["OpenTelemetry:Otlp:Endpoint"]
                ?? "http://otel-collector:4317";

            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("service.name", applicationName)
                .Enrich.With(services.GetRequiredService<CorrelationLogEnricher>())
                .WriteTo.Console()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = applicationName,
                        ["service.version"] = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0"
                    };
                });
        });

        return services;
    }
}