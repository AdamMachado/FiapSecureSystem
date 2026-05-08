using System.Net;
using System.Text.Json;

namespace Fiap.SecureAnalyzer.ApiGateway.Services.Common;

public sealed class UpstreamServiceException : Exception
{
    public UpstreamServiceException(
        string serviceName,
        HttpStatusCode statusCode,
        string? code,
        string message)
        : base(message)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
        Code = code;
    }

    public string ServiceName { get; }
    public HttpStatusCode StatusCode { get; }
    public string? Code { get; }

    public static async Task ThrowIfUnsuccessfulAsync(
        HttpResponseMessage response,
        string serviceName,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw Create(serviceName, response.StatusCode, payload);
    }

    public static UpstreamServiceException Create(
        string serviceName,
        HttpStatusCode statusCode,
        string? payload)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                var message =
                    TryGetString(root, "detail")
                    ?? TryGetString(root, "message")
                    ?? TryGetString(root, "title");

                var code = TryGetString(root, "code");

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return new UpstreamServiceException(serviceName, statusCode, code, message);
                }
            }
            catch (JsonException)
            {
            }

            return new UpstreamServiceException(serviceName, statusCode, null, payload);
        }

        return new UpstreamServiceException(
            serviceName,
            statusCode,
            null,
            $"The upstream service '{serviceName}' returned status code {(int)statusCode}.");
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.GetRawText();
    }
}
