using System.Net;

namespace Fiap.SecureSystem.WebApp.Clients.Identity;

public sealed class IdentityServiceClientException : Exception
{
    public IdentityServiceClientException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
