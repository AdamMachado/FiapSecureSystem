using Microsoft.EntityFrameworkCore;
using ReportService.Domain.Entities;
using System.Reflection.Emit;

namespace ReportService.Infrastructure.Persistence.Context;

public sealed class ReportDbContext : DbContext
{
    public ReportDbContext(DbContextOptions<ReportDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisReport> AnalysisReports => Set<AnalysisReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("report");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}