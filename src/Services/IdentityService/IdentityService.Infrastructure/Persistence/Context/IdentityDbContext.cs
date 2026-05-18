using IdentityService.Infrastructure.Configuration.Options;
using IdentityService.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Persistence.Context;

public sealed class IdentityDbContext : DbContext
{
    private readonly string _schema;

    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _schema = databaseOptions.Value.Schema;
    }

    public DbSet<IdentityUserRecord> Users => Set<IdentityUserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
