using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Observability.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSharedOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        params string[] activitySourceNames)
    {
        var serviceVersion =
            configuration[TelemetryConstants.ServiceVersionConfigKey] ??
            TelemetryConstants.DefaultServiceVersion;

        var otlpEndpoint = configuration[TelemetryConstants.OtlpEndpointConfigKey];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                foreach (var activitySourceName in activitySourceNames.Distinct())
                {
                    tracing.AddSource(activitySourceName);
                }

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }
}