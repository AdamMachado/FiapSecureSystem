using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcessingService.Domain.Entities;
using ProcessingService.Infrastructure.Configuration.Options;

namespace ProcessingService.Infrastructure.Persistence.Context;

public sealed class ProcessingDbContext : DbContext
{
    private DatabaseOptions _databaseOptions;

    public ProcessingDbContext(
    DbContextOptions<ProcessingDbContext> options,
    IOptions<DatabaseOptions> databaseOptions)
    : base(options)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public DbSet<AnalysisProcess> AnalysisProcesses => Set<AnalysisProcess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_databaseOptions.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
