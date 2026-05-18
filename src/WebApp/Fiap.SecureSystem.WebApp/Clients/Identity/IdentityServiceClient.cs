using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

namespace Fiap.SecureSystem.WebApp.Clients.Identity;

public sealed class IdentityServiceClient(HttpClient httpClient) : IIdentityServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<LoginResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var payload = new LoginRequest(email, password);

        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync("/api/auth/login", payload, SerializerOptions, cancellationToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<LoginResponse>(SerializerOptions, cancellationToken)
            ?? throw new IdentityServiceClientException(HttpStatusCode.BadGateway, "IdentityService returned an empty login response.");
    }

    public async Task<LoginResponse> RegisterAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        var payload = new RegisterRequest(email, displayName, password);

        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync("/api/auth/register", payload, SerializerOptions, cancellationToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<LoginResponse>(SerializerOptions, cancellationToken)
            ?? throw new IdentityServiceClientException(HttpStatusCode.BadGateway, "IdentityService returned an empty register response.");
    }

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
            throw new IdentityServiceClientException(
                HttpStatusCode.BadGateway,
                $"Nao foi possivel conectar ao IdentityService. Verifique se o servico esta em execucao. Detalhes: {exception.Message}");
        }
    }

    private static async Task<IdentityServiceClientException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var message = $"IdentityService request failed with status code {(int)response.StatusCode}.";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<IdentityProblemDetails>(SerializerOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                message = problem.Detail;
            }
        }
        catch
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                message = body;
            }
        }

        return new IdentityServiceClientException(response.StatusCode, message);
    }
}
