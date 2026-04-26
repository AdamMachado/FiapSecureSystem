using ProcessingService.Application;
using ProcessingService.Infrastructure.Configuration;
using ProcessingService.Worker;
using Shared.Observability.Correlation;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

const string serviceName = ActivitySources.ProcessingService;

builder.Services.AddSharedSerilog(
    builder.Configuration,
    serviceName);

builder.Services.AddCorrelationContext();

builder.Services.AddProcessingApplication();
builder.Services.AddProcessingInfrastructure(builder.Configuration);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName,
    ActivitySources.ProcessingService);

builder.Services.AddHostedService<WorkerLifecycleLogger>();

var host = builder.Build();

await host.RunAsync();
