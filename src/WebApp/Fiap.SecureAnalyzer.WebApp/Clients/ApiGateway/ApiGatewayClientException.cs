using System.Net;

namespace Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway;

public sealed class ApiGatewayClientException : Exception
{
    public ApiGatewayClientException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
