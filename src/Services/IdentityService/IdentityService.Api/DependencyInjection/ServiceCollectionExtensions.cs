using IdentityService.Application;

namespace IdentityService.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddScoped<Middlewares.ExceptionHandlingMiddleware>();
        services.AddIdentityApplication();
        return services;
    }
}
