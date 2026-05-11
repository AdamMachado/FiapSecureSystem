using Fiap.SecureAnalyzer.ApiGateway.Options;
using Fiap.SecureAnalyzer.ApiGateway.Services.Report;
using Fiap.SecureAnalyzer.ApiGateway.Services.Upload;
using Microsoft.Extensions.Options;

namespace Fiap.SecureAnalyzer.ApiGateway.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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
