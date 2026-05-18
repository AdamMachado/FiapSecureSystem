using Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

namespace Fiap.SecureSystem.WebApp.Clients.Identity;

public interface IIdentityServiceClient
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken);
}
