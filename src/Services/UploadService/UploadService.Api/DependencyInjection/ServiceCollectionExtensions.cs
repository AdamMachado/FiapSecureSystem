using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using UploadService.Api.Middlewares;
using UploadService.Api.Services;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.UseCases.CreateAnalysis;
using UploadService.Application.UseCases.GetAnalysisStatus;
using UploadService.Application.UseCases.UpdateAnalysisStatus;

namespace UploadService.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUploadApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        services.Configure<JsonOptions>(options => ConfigureJson(options.SerializerOptions));
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<CreateAnalysisValidator>();
        services.AddScoped<CreateAnalysisHandler>();
        services.AddScoped<GetAnalysisStatusHandler>();
        services.AddScoped<UpdateAnalysisStatusHandler>();

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
}
