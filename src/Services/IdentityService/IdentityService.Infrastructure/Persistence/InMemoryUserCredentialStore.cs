using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Domain.Entities;
using IdentityService.Domain.ValueObjects;
using IdentityService.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Persistence;

public sealed class InMemoryUserCredentialStore : IUserCredentialStore
{
    private readonly Dictionary<string, UserAuthenticationInfo> _usersByEmail;

    public InMemoryUserCredentialStore(
        IOptions<IdentityOptions> options,
        IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        _usersByEmail = options.Value.SeedUsers
            .Select(user => CreateUserAuthenticationInfo(user, passwordHasher))
            .ToDictionary(
                user => user.User.Email.Value,
                user => user,
                StringComparer.OrdinalIgnoreCase);
    }

    public Task<UserAuthenticationInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<UserAuthenticationInfo?>(null);

        _usersByEmail.TryGetValue(email.Trim().ToLowerInvariant(), out var user);
        return Task.FromResult(user);
    }

    private static UserAuthenticationInfo CreateUserAuthenticationInfo(
        SeedUserOptions seedUser,
        IPasswordHasher passwordHasher)
    {
        var user = User.Create(
            seedUser.Id == Guid.Empty ? Guid.NewGuid() : seedUser.Id,
            EmailAddress.Create(seedUser.Email),
            seedUser.DisplayName,
            seedUser.Roles,
            seedUser.Scopes,
            seedUser.IsActive);

        var passwordHash = passwordHasher.Hash(seedUser.Password);

        return new UserAuthenticationInfo(user, passwordHash);
    }
}
