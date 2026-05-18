using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Infrastructure.Authentication;
using IdentityService.Infrastructure.Configuration.Options;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services
            .AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .Validate(options => options.AccessTokenExpirationMinutes > 0, "Identity:AccessTokenExpirationMinutes must be greater than zero.")
            .Validate(options => options.SeedUsers.Count > 0, "Identity:SeedUsers must contain at least one user.")
            .ValidateOnStart();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(
                databaseOptions.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

            if (databaseOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IUserCredentialStore, EfUserCredentialStore>();
        services.AddHostedService<IdentityDatabaseInitializerHostedService>();

        return services;
    }
}
