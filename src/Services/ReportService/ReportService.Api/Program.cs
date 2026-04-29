using Serilog;
using ReportService.Api.Configuration;
using ReportService.Api.DependencyInjection;
using ReportService.Api.Middlewares;
using ReportService.Infrastructure.Configuration;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["OpenTelemetry:ServiceName"]
    ?? ActivitySources.ReportService;

builder.Services.AddSharedSerilog(builder.Configuration, serviceName);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName: serviceName,
    serviceName);

builder.Services.AddReportProblemDetails();
builder.Services.AddReportSwagger();

builder.Services
    .AddReportApiServices()
    .AddReportInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();

if (app.Environment.IsDevelopment())
    app.UseReportSwagger();

//app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.UseSharedHealthChecks();

app.Run();