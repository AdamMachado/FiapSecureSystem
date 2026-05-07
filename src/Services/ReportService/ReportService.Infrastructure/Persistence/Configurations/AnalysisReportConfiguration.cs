using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportService.Domain.Entities;

namespace ReportService.Infrastructure.Persistence.Configurations;

public sealed class AnalysisReportConfiguration : IEntityTypeConfiguration<AnalysisReport>
{
    public void Configure(EntityTypeBuilder<AnalysisReport> builder)
    {
        builder.ToTable("analysis_reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AnalysisRequestId)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .IsRequired();

        builder.Property(x => x.AnalysisData)
            .HasColumnName("analysis_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Files)
            .WithOne()
            .HasForeignKey(x => x.AnalysisReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Files)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.AnalysisRequestId)
            .IsUnique();

        builder.HasIndex(x => x.RequestedByUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
