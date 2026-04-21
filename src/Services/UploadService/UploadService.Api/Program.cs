using Serilog;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using Shared.Observability.Logging;
using Shared.Observability.Telemetry;
using UploadService.Api.Configuration;
using UploadService.Api.DependencyInjection;
using UploadService.Api.Middlewares;
using UploadService.Application;
using UploadService.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

var projectIdentifier = "UploadService.Api";

builder.Services.AddSharedSerilog(builder.Configuration, projectIdentifier);
builder.Services
    .AddUploadApi()
    .AddUploadProblemDetails()
    .AddUploadSwagger()
    .AddSharedOpenTelemetry(
        builder.Configuration,
        projectIdentifier,
        ActivitySources.UploadService)
    .AddUploadInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();

if (app.Environment.IsDevelopment())
    app.UseUploadSwagger();

app.UseHttpsRedirection();
app.MapControllers();
app.UseSharedHealthChecks();

app.Run();
