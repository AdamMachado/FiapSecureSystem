namespace ReportService.Api.Configuration;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddReportProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }
}