using Fiap.SecureSystem.ApiGateway.Contracts.Responses;

namespace Fiap.SecureSystem.ApiGateway.Services.Identity;

public interface IIdentityServiceClient
{
    Task<LoginResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
