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

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Format)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.GeneratedAtUtc);
        builder.Property(x => x.FailedAtUtc);

        builder.OwnsOne(x => x.Content, content =>
        {
            content.Property(x => x.Value)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();
        });

        builder.OwnsOne(x => x.GeneratedFileLocation, location =>
        {
            location.Property(x => x.BucketName)
                .HasColumnName("storage_bucket")
                .HasMaxLength(100);

            location.Property(x => x.ObjectKey)
                .HasColumnName("storage_object_key")
                .HasMaxLength(512);

            location.Property(x => x.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(255);
        });

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.AnalysisRequestId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}