using System.Net;
using System.Text.Json;
using Shared.Kernel.Exceptions;

namespace ReportService.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, code, message) = exception switch
        {
            ValidationException ex => ((int)HttpStatusCode.BadRequest, ex.Code, ex.Message),
            NotFoundException ex => ((int)HttpStatusCode.NotFound, ex.Code, ex.Message),
            AppException ex => ((int)HttpStatusCode.BadRequest, ex.Code, ex.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "internal_error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;

        var payload = new
        {
            error = new
            {
                code,
                message
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}