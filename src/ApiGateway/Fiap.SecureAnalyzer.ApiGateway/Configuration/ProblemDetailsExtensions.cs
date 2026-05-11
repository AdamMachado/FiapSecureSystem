namespace Fiap.SecureAnalyzer.ApiGateway.Configuration;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApiGatewayProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }
}
