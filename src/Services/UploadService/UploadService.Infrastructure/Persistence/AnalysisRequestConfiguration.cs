using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UploadService.Domain.Entities;
namespace UploadService.Infrastructure.Persistence.Configurations;

public sealed class AnalysisRequestConfiguration : IEntityTypeConfiguration<AnalysisRequest>
{
    public void Configure(EntityTypeBuilder<AnalysisRequest> builder)
    {
        builder.ToTable("analysis_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.RequestedByUserId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.StartedAtUtc);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.FailedAtUtc);

        builder.OwnsOne(x => x.FileMetadata, metadata =>
        {
            metadata.Property(x => x.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(255)
                .IsRequired();

            metadata.Property(x => x.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(128)
                .IsRequired();

            metadata.Property(x => x.SizeInBytes)
                .HasColumnName("size_in_bytes")
                .IsRequired();

            metadata.Property(x => x.FileType)
                .HasColumnName("file_type")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsOne(x => x.FileHash, hash =>
        {
            hash.Property(x => x.Value)
                .HasColumnName("file_hash")
                .HasMaxLength(64)
                .IsRequired();
        });

        builder.OwnsOne(x => x.StorageLocation, location =>
        {
            location.Property(x => x.BucketName)
                .HasColumnName("storage_bucket")
                .HasMaxLength(100)
                .IsRequired();

            location.Property(x => x.ObjectKey)
                .HasColumnName("storage_object_key")
                .HasMaxLength(512)
                .IsRequired();
        });

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex("RequestedByUserId");
        builder.HasIndex("Status");
        builder.HasIndex("CreatedAtUtc");
    }
}
