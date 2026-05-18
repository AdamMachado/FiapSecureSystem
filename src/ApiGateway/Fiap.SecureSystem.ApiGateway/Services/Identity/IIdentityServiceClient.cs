using Fiap.SecureSystem.ApiGateway.Contracts.Responses;

namespace Fiap.SecureSystem.ApiGateway.Services.Identity;

public interface IIdentityServiceClient
{
    Task<LoginResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<LoginResponse> RegisterAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken);
}
