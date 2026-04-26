using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UploadService.Domain.Entities;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.Persistence.Context;

public sealed class UploadDbContext : DbContext
{
    private readonly string _schema;

    public UploadDbContext(DbContextOptions<UploadDbContext> options, IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _schema = databaseOptions.Value.Schema;
    }

    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UploadDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
