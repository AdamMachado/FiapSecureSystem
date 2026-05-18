using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Domain.Entities;
using IdentityService.Domain.ValueObjects;
using IdentityService.Infrastructure.Persistence.Context;
using IdentityService.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public sealed class EfUserCredentialStore : IUserCredentialStore
{
    private readonly IdentityDbContext _dbContext;

    public EfUserCredentialStore(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAuthenticationInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var record = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (record is null)
            return null;

        var user = User.Create(
            record.Id,
            EmailAddress.Create(record.Email),
            record.DisplayName,
            SplitCsv(record.RolesCsv),
            SplitCsv(record.ScopesCsv),
            record.IsActive);

        return new UserAuthenticationInfo(user, record.PasswordHash);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task CreateAsync(
        Guid id,
        string email,
        string displayName,
        string passwordHash,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var record = new IdentityUserRecord
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            RolesCsv = string.Join(',', roles),
            ScopesCsv = string.Join(',', scopes),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _dbContext.Users.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<string> SplitCsv(string value)
        => value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
