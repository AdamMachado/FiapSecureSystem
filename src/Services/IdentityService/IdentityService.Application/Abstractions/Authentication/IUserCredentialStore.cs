namespace IdentityService.Application.Abstractions.Authentication;

public interface IUserCredentialStore
{
    Task<UserAuthenticationInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
