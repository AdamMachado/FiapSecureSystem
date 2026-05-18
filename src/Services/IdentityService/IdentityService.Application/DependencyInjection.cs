using IdentityService.Application.UseCases.Login;
using IdentityService.Application.UseCases.Register;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        return services;
    }
}
