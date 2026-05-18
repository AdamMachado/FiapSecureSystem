using IdentityService.Application.UseCases.Login;

namespace IdentityService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        return services;
    }
}
