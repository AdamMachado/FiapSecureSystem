using Microsoft.EntityFrameworkCore;
using ProcessingService.Domain.Entities;

namespace ProcessingService.Infrastructure.Persistence.Context;

public sealed class ProcessingDbContext : DbContext
{
    public ProcessingDbContext(DbContextOptions<ProcessingDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisProcess> AnalysisProcesses => Set<AnalysisProcess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
