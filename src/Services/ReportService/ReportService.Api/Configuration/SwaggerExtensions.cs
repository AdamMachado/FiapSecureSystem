namespace ReportService.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddReportSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Report Service API",
                Version = "v1"
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