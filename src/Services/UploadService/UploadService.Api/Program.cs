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

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationContext();

if (app.Environment.IsDevelopment())
    app.UseUploadSwagger();

//app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.UseSharedHealthChecks();

app.Run();
