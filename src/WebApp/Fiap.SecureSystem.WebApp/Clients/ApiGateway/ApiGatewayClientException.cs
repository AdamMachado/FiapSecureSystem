using System.Net;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway;

public sealed class ApiGatewayClientException : Exception
{
    public ApiGatewayClientException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
