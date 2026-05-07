using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportService.Domain.Entities;

namespace ReportService.Infrastructure.Persistence.Configurations;

public sealed class AnalysisReportFileConfiguration : IEntityTypeConfiguration<AnalysisReportFile>
{
    public void Configure(EntityTypeBuilder<AnalysisReportFile> builder)
    {
        builder.ToTable("analysis_report_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AnalysisReportId)
            .IsRequired();

        builder.Property(x => x.Format)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.BucketName)
            .HasColumnName("bucket_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(x => new { x.AnalysisReportId, x.Format })
            .IsUnique();

        builder.HasIndex(x => x.Format);
    }
}
