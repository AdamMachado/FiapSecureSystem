using ReportService.Api.Configuration;
using ReportService.Api.DependencyInjection;
using ReportService.Api.Middlewares;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedSerilog(builder.Configuration, "ReportService.Api");

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName: "ReportService.Api",
    ActivitySources.ReportService);

builder.Services.AddReportProblemDetails();
builder.Services.AddReportSwagger();
builder.Services.AddReportApiServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCorrelationContext();

app.UseReportSwagger();

app.UseAuthorization();

app.MapControllers();
app.UseSharedHealthChecks();

app.Run();