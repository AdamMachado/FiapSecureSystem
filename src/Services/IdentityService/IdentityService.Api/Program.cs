using System.Diagnostics;
using IdentityService.Api.Configuration;
using IdentityService.Api.DependencyInjection;
using IdentityService.Api.Middlewares;
using IdentityService.Infrastructure.Configuration;
using Serilog;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;
using Shared.Security.DependencyInjection;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["OpenTelemetry:ServiceName"]
    ?? "SOAT.IdentityService.Api";

builder.Services.AddCorrelationContext();
builder.Services.AddSharedSerilog(builder.Configuration, serviceName);
builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName,
    serviceName);
builder.Services.AddSharedHealthChecks();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedJwtAuthorization();
builder.Services.AddIdentityProblemDetails();
builder.Services.AddIdentitySwagger();

builder.Services
    .AddIdentityApiServices()
    .AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();
var isRunningInContainer =
    string.Equals(
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
        "true",
        StringComparison.OrdinalIgnoreCase);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Starting application {ApplicationName} in environment: {Environment}",
    serviceName,
    app.Environment.EnvironmentName);

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.UseIdentitySwagger();

if (!isRunningInContainer)
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
