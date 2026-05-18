using ReportService.Application;

namespace ReportService.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddReportApplication();

        return services;
    }
}
