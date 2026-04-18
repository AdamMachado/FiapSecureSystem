using Microsoft.EntityFrameworkCore;
using UploadService.Domain.Entities;

namespace UploadService.Infrastructure.Persistence.Context;

public sealed class UploadDbContext : DbContext
{
    public UploadDbContext(DbContextOptions<UploadDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("upload");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UploadDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
