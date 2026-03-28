using FiapSecureSystem.UploadOrchestration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FiapSecureSystem.UploadOrchestration.Infrastructure.Persistence;

public class UploadDbContext : DbContext
{
    public UploadDbContext(DbContextOptions<UploadDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisRequest>(entity =>
        {
            entity.ToTable("analysis_requests");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);
            entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}