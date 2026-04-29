using Serilog;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;
using UploadService.Api.Configuration;
using UploadService.Api.DependencyInjection;
using UploadService.Api.Middlewares;
using UploadService.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

var projectIdentifier = "UploadService.Api";

builder.Services.AddSharedSerilog(builder.Configuration, projectIdentifier);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    projectIdentifier,
    ActivitySources.UploadService);

builder.Services.AddUploadProblemDetails();
builder.Services.AddUploadSwagger();

builder.Services
    .AddUploadApiServices(builder.Configuration)
    .AddUploadInfrastructure(builder.Configuration);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Starting application {ApplicationName} in environment: {Environment}",
    projectIdentifier,
    app.Environment.EnvironmentName);

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();

if (app.Environment.IsDevelopment())
    app.UseUploadSwagger();

//app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.UseSharedHealthChecks();

try
{
    logger.LogInformation("Application {ApplicationName} started", projectIdentifier);

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application {ApplicationName} terminated unexpectedly", projectIdentifier);
    throw;
}
finally
{
    logger.LogInformation("Application {ApplicationName} stopped", projectIdentifier);
}
