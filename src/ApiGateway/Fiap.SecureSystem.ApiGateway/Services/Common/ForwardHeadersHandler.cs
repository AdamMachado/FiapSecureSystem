using System.Net.Http.Headers;
using Shared.Contracts.Messaging;

namespace Fiap.SecureSystem.ApiGateway.Services.Common;

public sealed class ForwardHeadersHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardHeadersHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            ForwardAuthorizationHeader(httpContext, request);
            ForwardHeader(httpContext, request, HeaderNames.CorrelationId);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static void ForwardAuthorizationHeader(HttpContext httpContext, HttpRequestMessage request)
    {
        var authorization = httpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
            return;

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
    }

    private static void ForwardHeader(HttpContext httpContext, HttpRequestMessage request, string headerName)
    {
        if (!httpContext.Request.Headers.TryGetValue(headerName, out var values))
            return;

        request.Headers.Remove(headerName);
        request.Headers.TryAddWithoutValidation(headerName, values.ToArray());
    }
}
