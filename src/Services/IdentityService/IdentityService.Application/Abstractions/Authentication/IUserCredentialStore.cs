namespace IdentityService.Application.Abstractions.Authentication;

public interface IUserCredentialStore
{
    Task<UserAuthenticationInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task CreateAsync(
        Guid id,
        string email,
        string displayName,
        string passwordHash,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken);
}
