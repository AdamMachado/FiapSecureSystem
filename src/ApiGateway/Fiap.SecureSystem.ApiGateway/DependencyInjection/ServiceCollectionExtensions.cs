using System.Text.Json.Serialization;
using Fiap.SecureSystem.ApiGateway.Options;
using Fiap.SecureSystem.ApiGateway.Services.Common;
using Fiap.SecureSystem.ApiGateway.Services.Identity;
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
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardHeadersHandler>();

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

        services
            .AddOptions<IdentityServiceOptions>()
            .Bind(configuration.GetSection(IdentityServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IUploadServiceClient, UploadServiceClient>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<UploadServiceOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .AddHttpMessageHandler<ForwardHeadersHandler>();

        services
            .AddHttpClient<IReportServiceClient, ReportServiceClient>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<ReportServiceOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .AddHttpMessageHandler<ForwardHeadersHandler>();

        services
            .AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<IdentityServiceOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .AddHttpMessageHandler<ForwardHeadersHandler>();

        return services;
    }
}
