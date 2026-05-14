using System.Text.Json.Serialization;
using Fiap.SecureSystem.ApiGateway.Options;
using Fiap.SecureSystem.ApiGateway.Services.Report;
using Fiap.SecureSystem.ApiGateway.Services.Upload;
using Microsoft.Extensions.Options;

namespace Fiap.SecureSystem.ApiGateway.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddEndpointsApiExplorer();

        services
            .AddOptions<UploadServiceOptions>()
            .Bind(configuration.GetSection(UploadServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<ReportServiceOptions>()
            .Bind(configuration.GetSection(ReportServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IUploadServiceClient, UploadServiceClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<UploadServiceOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        });

        services.AddHttpClient<IReportServiceClient, ReportServiceClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<ReportServiceOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        });

        return services;
    }
}
