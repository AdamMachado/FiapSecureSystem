using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Infrastructure.Authentication;
using IdentityService.Infrastructure.Configuration.Options;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .Validate(options => options.AccessTokenExpirationMinutes > 0, "Identity:AccessTokenExpirationMinutes must be greater than zero.")
            .Validate(options => options.SeedUsers.Count > 0, "Identity:SeedUsers must contain at least one user.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IUserCredentialStore, InMemoryUserCredentialStore>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        return services;
    }
}
