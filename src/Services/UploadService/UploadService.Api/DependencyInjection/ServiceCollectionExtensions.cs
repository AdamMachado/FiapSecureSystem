using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using UploadService.Api.Middlewares;
using UploadService.Api.Services;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application;

namespace UploadService.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUploadApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        services.Configure<JsonOptions>(options => ConfigureJson(options.SerializerOptions));
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddUploadApplication();

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
}
