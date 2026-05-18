using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Infrastructure.Configuration.Options;
using IdentityService.Infrastructure.Persistence.Context;
using IdentityService.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Persistence;

public sealed class IdentityDatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly ILogger<IdentityDatabaseInitializerHostedService> _logger;

    public IdentityDatabaseInitializerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<IdentityOptions> identityOptions,
        ILogger<IdentityDatabaseInitializerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _databaseOptions = databaseOptions;
        _identityOptions = identityOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await EnsureSchemaAsync(dbContext, cancellationToken);
        await EnsureUsersTableAsync(dbContext, cancellationToken);
        await SeedUsersAsync(dbContext, passwordHasher, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureSchemaAsync(IdentityDbContext dbContext, CancellationToken cancellationToken)
    {
        var schema = _databaseOptions.Value.Schema;
        await dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {QuoteIdentifier(schema)};", cancellationToken);
    }

    private async Task EnsureUsersTableAsync(IdentityDbContext dbContext, CancellationToken cancellationToken)
    {
        var schema = QuoteIdentifier(_databaseOptions.Value.Schema);

        var sql = $"""
            CREATE TABLE IF NOT EXISTS {schema}.users
            (
                id uuid PRIMARY KEY,
                email character varying(320) NOT NULL,
                display_name character varying(200) NOT NULL,
                password_hash character varying(512) NOT NULL,
                roles character varying(1000) NOT NULL,
                scopes character varying(2000) NOT NULL,
                is_active boolean NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON {schema}.users (email);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task SeedUsersAsync(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var configuredUsers = _identityOptions.Value.SeedUsers;

        if (configuredUsers.Count == 0)
            return;

        var existingEmails = await dbContext.Users
            .AsNoTracking()
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);

        var existingEmailSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        var recordsToInsert = configuredUsers
            .Where(user => !existingEmailSet.Contains(user.Email.Trim().ToLowerInvariant()))
            .Select(user => new IdentityUserRecord
            {
                Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id,
                Email = user.Email.Trim().ToLowerInvariant(),
                DisplayName = user.DisplayName.Trim(),
                PasswordHash = passwordHasher.Hash(user.Password),
                RolesCsv = string.Join(',', user.Roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).Distinct(StringComparer.Ordinal)),
                ScopesCsv = string.Join(',', user.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).Select(scope => scope.Trim()).Distinct(StringComparer.Ordinal)),
                IsActive = user.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            })
            .ToArray();

        if (recordsToInsert.Length == 0)
            return;

        await dbContext.Users.AddRangeAsync(recordsToInsert, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {UserCount} identity users into schema {Schema}.", recordsToInsert.Length, _databaseOptions.Value.Schema);
    }

    private static string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
