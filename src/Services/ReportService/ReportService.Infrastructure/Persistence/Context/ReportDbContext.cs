using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReportService.Domain.Entities;
using ReportService.Infrastructure.Configuration.Options;

namespace ReportService.Infrastructure.Persistence.Context;

public sealed class ReportDbContext : DbContext
{
    private readonly string _schema;

    public ReportDbContext(DbContextOptions<ReportDbContext> options, IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _schema = databaseOptions.Value.Schema;
    }

    public DbSet<AnalysisReport> AnalysisReports => Set<AnalysisReport>();
    public DbSet<AnalysisReportFile> AnalysisReportFiles => Set<AnalysisReportFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
