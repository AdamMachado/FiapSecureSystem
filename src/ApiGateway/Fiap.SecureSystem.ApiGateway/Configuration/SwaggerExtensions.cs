using Microsoft.OpenApi;
using Shared.Contracts.Messaging;

namespace Fiap.SecureSystem.ApiGateway.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiGatewaySwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API Gateway",
                Version = "v1",
                Description = "Gateway para consolidacao das APIs de upload e relatorios."
            });

            options.AddSecurityDefinition(HeaderNames.CorrelationId, new OpenApiSecurityScheme
            {
                Name = HeaderNames.CorrelationId,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Correlation id propagado entre servicos. Se nao informado, sera gerado automaticamente."
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
                    new OpenApiSecuritySchemeReference("Bearer", null),
                    []
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiGatewaySwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        });

        return app;
    }
}
