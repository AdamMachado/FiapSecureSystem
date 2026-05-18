using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Security.Authentication;
using Shared.Security.Authorization;

namespace Shared.Security.DependencyInjection;

public static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        services
            .AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), $"{JwtOptions.SectionName}:SigningKey is required.")
            .Validate(options => options.ClockSkewSeconds >= 0, $"{JwtOptions.SectionName}:ClockSkewSeconds must be greater than or equal to zero.")
            .ValidateOnStart();

        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = jwtOptions.ValidateLifetime,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };
            });

        return services;
    }

    public static IServiceCollection AddSharedJwtAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.AnalysisRead, policy => RequireScope(policy, AuthorizationPolicies.AnalysisRead))
            .AddPolicy(AuthorizationPolicies.AnalysisWrite, policy => RequireScope(policy, AuthorizationPolicies.AnalysisWrite))
            .AddPolicy(AuthorizationPolicies.ReportRead, policy => RequireScope(policy, AuthorizationPolicies.ReportRead))
            .AddPolicy(AuthorizationPolicies.AnalysisDetailsRead, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.HasScope(AuthorizationPolicies.AnalysisRead)
                    && context.User.HasScope(AuthorizationPolicies.ReportRead));
            });

        return services;
    }

    private static void RequireScope(AuthorizationPolicyBuilder policy, string scope)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.HasScope(scope));
    }
}
