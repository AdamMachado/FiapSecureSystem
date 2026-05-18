using System.Text;
using System.Text.Json;
using Fiap.SecureSystem.ApiGateway.Contracts.Requests;
using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Serialization;
using Fiap.SecureSystem.ApiGateway.Services.Common;

namespace Fiap.SecureSystem.ApiGateway.Services.Identity;

public sealed class IdentityServiceClient : IIdentityServiceClient
{
    private const string ServiceName = "IdentityService.Api";
    private readonly HttpClient _httpClient;

    public IdentityServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var request = new LoginRequest(email, password);
        using var content = CreateJsonContent(request);
        using var response = await SendAsync(
            () => _httpClient.PostAsync("/api/auth/login", content, cancellationToken),
            cancellationToken);

        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<LoginResponse>(response, cancellationToken);
    }

    public async Task<LoginResponse> RegisterAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        var request = new RegisterRequest(email, displayName, password);
        using var content = CreateJsonContent(request);
        using var response = await SendAsync(
            () => _httpClient.PostAsync("/api/auth/register", content, cancellationToken),
            cancellationToken);

        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<LoginResponse>(response, cancellationToken);
    }

    private static StringContent CreateJsonContent<T>(T payload)
        => new(JsonSerializer.Serialize(payload, JsonDefaults.Options), Encoding.UTF8, "application/json");

    private static async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UpstreamServiceException(
                ServiceName,
                System.Net.HttpStatusCode.BadGateway,
                "UPSTREAM_CONNECTION_FAILED",
                $"Nao foi possivel conectar ao {ServiceName}. Verifique se o servico esta em execucao. Detalhes: {exception.Message}");
        }
    }

    private static async Task<T> ReadFromJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options, cancellationToken);

        if (value is null)
        {
            throw new InvalidOperationException("The upstream service returned an empty response body.");
        }

        return value;
    }
}
