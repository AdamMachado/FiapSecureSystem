using Microsoft.OpenApi;
using Shared.Contracts.Messaging;

namespace IdentityService.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddIdentitySwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Identity Service API",
                Version = "v1",
                Description = "API responsavel por autenticar usuarios e emitir JWT."
            });

            options.AddSecurityDefinition(HeaderNames.CorrelationId, new OpenApiSecurityScheme
            {
                Name = HeaderNames.CorrelationId,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Correlation id propagado entre servicos."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(HeaderNames.CorrelationId, null),
                    []
                },
                {
                    new OpenApiSecuritySchemeReference("Bearer", SecuritySchemeType.Http),
                    []
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseIdentitySwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1");
        });

        return app;
    }
}
