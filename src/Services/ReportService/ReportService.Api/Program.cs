using ReportService.Api.Configuration;
using ReportService.Api.DependencyInjection;
using ReportService.Api.Middlewares;
using ReportService.Infrastructure.Configuration;
using Serilog;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;
using System.Diagnostics;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["OpenTelemetry:ServiceName"]
    ?? ActivitySources.ReportService;

builder.Services.AddSharedSerilog(builder.Configuration, serviceName);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName,
    serviceName);

builder.Services.AddReportProblemDetails();
builder.Services.AddReportSwagger();

builder.Services
    .AddReportApiServices()
    .AddReportInfrastructure(builder.Configuration);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Starting application {ApplicationName} in environment: {Environment}",
    serviceName,
    app.Environment.EnvironmentName);

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();

if (app.Environment.IsDevelopment())
    app.UseReportSwagger();

//app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.UseSharedHealthChecks();

try
{
    logger.LogInformation("Application {ApplicationName} started", serviceName);

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application {ApplicationName} terminated unexpectedly", serviceName);
    throw;
}
finally
{
    logger.LogInformation("Application {ApplicationName} stopped", serviceName);
}
