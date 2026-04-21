using ReportService.Application;
using ReportService.Infrastructure.Configuration;

namespace ReportService.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddReportApplication();
        services.AddReportInfrastructure(configuration);

        return services;
    }
}