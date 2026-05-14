using Shared.Contracts.Messaging;
using Microsoft.OpenApi;

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
                Description = "Gateway para consolidação das APIs de upload e relatórios."
            });

            options.AddSecurityDefinition(HeaderNames.CorrelationId, new OpenApiSecurityScheme
            {
                Name = HeaderNames.CorrelationId,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Correlation id propagado entre serviços. Se não informado, será gerado automaticamente."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(HeaderNames.CorrelationId, null),
                    new List<string>()
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
