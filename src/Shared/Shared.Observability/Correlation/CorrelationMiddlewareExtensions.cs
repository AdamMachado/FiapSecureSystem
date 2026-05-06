using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using Shared.Contracts.Messaging;
using System.Diagnostics;

namespace Shared.Observability.Correlation;

public static class CorrelationMiddlewareExtensions
{
    public static IServiceCollection AddCorrelationContext(this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationContext(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var accessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();

            var correlationId = GetHeaderOrGenerate(context, HeaderNames.CorrelationId);
            var causationId = GetOptionalHeader(context, HeaderNames.CausationId);

            accessor.CorrelationId = correlationId;
            accessor.CausationId = causationId;

            context.Response.Headers[HeaderNames.CorrelationId] = correlationId;

            Activity.Current?.AddTag("correlation.id", correlationId);

            if(!string.IsNullOrEmpty(causationId))
            {
                context.Response.Headers[HeaderNames.CausationId] = causationId;
                Activity.Current?.AddTag("causation.id", causationId);
            }

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("CausationId", causationId ?? string.Empty))
            {
                try
                {
                    await next();
                }
                finally
                {
                    accessor.Clear();
                }
            }
        });
    }

    private static string GetHeaderOrGenerate(HttpContext context, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var values) &&
            !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return values.First()!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static string? GetOptionalHeader(HttpContext context, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var values) &&
            !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return values.First()!;
        }

        return null;
    }
}