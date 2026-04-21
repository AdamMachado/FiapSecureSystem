using Microsoft.OpenApi;
using Shared.Contracts.Messaging;

namespace UploadService.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddUploadSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UploadService API",
                Version = "v1",
                Description = "API responsável por receber diagramas arquiteturais e expor o status da análise."
            });

            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "binary"
            });

            options.AddSecurityDefinition(HeaderNames.CorrelationId, new OpenApiSecurityScheme
            {
                Name = HeaderNames.CorrelationId,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Correlation id propagado entre serviços. Se não informado, será gerado pelo middleware."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecuritySchemeReference(HeaderNames.CorrelationId, null),
                    new List<string>()
                }
            });
        });

        return services;
    }

    public static WebApplication UseUploadSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}