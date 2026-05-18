using Microsoft.OpenApi;
using Shared.Contracts.Messaging;

namespace ReportService.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddReportSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Report Service API",
                Version = "v1",
                Description = "API responsavel por expor relatorios gerados para uma analise."
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
                    new OpenApiSecuritySchemeReference("Bearer", null),
                    []
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseReportSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Report Service API v1");
        });

        return app;
    }
}
