using ProcessingService.Application;
using ProcessingService.Infrastructure.Configuration;
using ProcessingService.Worker;
using Shared.Observability.Correlation;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;
using System.Diagnostics;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var serviceName =
    builder.Configuration["OpenTelemetry:ServiceName"]
    ?? ActivitySources.ProcessingService;

builder.Services.AddCorrelationContext();
builder.Services.AddSharedSerilog(builder.Configuration, serviceName);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName,
    ActivitySources.ProcessingService);

builder.Services.AddProcessingApplication();
builder.Services.AddProcessingInfrastructure(builder.Configuration);

builder.Services.AddHostedService<WorkerLifecycleLogger>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var environment = host.Services.GetRequiredService<IHostEnvironment>();

logger.LogInformation(
    "Starting worker {ApplicationName} in {Environment}",
    serviceName,
    environment.EnvironmentName);

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Worker {ApplicationName} terminated unexpectedly", "ProcessingService.Worker");
    throw;
}
finally
{
    logger.LogInformation("Worker {ApplicationName} stopped", "ProcessingService.Worker");
}