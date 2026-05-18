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
                Description = "API responsavel por receber diagramas arquiteturais e expor o status da analise."
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
                Description = "Correlation id propagado entre servicos. Se nao informado, sera gerado pelo middleware."
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

    public static WebApplication UseUploadSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
