using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Infrastructure.Persistence.Configurations;

public sealed class AnalysisProcessConfiguration : IEntityTypeConfiguration<AnalysisProcess>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AnalysisProcess> builder)
    {
        builder.ToTable("analysis_processes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.OwnsOne(x => x.AnalysisRequestId, requestId =>
        {
            requestId.Property(x => x.Value)
                .HasColumnName("analysis_request_id")
                .IsRequired();
        });

        builder.Property(x => x.RequestedByUserId).IsRequired();

        builder.OwnsOne(x => x.SourceFileLocation, location =>
        {
            location.Property(x => x.BucketName)
                .HasColumnName("source_bucket")
                .HasMaxLength(100)
                .IsRequired();

            location.Property(x => x.ObjectKey)
                .HasColumnName("source_object_key")
                .HasMaxLength(512)
                .IsRequired();
        });

        builder.Property(x => x.DiagramType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(x => x.ExtractedText, extractedText =>
        {
            extractedText.Property(x => x.Value)
                .HasColumnName("extracted_text")
                .HasColumnType("text");
        });

        builder.OwnsOne(x => x.ResultSummary, summary =>
        {
            summary.Property(x => x.Overview)
                .HasColumnName("summary_overview")
                .HasColumnType("text");

            summary.Property(x => x.TotalComponents)
                .HasColumnName("summary_total_components");

            summary.Property(x => x.TotalRisks)
                .HasColumnName("summary_total_risks");

            summary.Property(x => x.TotalRecommendations)
                .HasColumnName("summary_total_recommendations");

            summary.Property(x => x.RequiresManualReview)
                .HasColumnName("summary_requires_manual_review");

            summary.Property(x => x.Warnings)
                .HasColumnName("summary_warnings")
                .HasColumnType("jsonb")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, JsonSerializerOptions),
                    value => JsonSerializer.Deserialize<IReadOnlyCollection<string>>(value, JsonSerializerOptions) ?? Array.Empty<string>())
                .Metadata.SetValueComparer(CreateStringCollectionComparer());
        });

        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.Property(x => x.FailureDetails).HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.StartedAtUtc);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.FailedAtUtc);

        builder.Property<List<IdentifiedComponentDto>>("_components")
            .HasColumnName("components")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonSerializerOptions),
                value => JsonSerializer.Deserialize<List<IdentifiedComponentDto>>(value, JsonSerializerOptions) ?? new List<IdentifiedComponentDto>())
            .Metadata.SetValueComparer(CreateCollectionComparer<IdentifiedComponentDto>());

        builder.Property<List<ArchitecturalRiskDto>>("_risks")
            .HasColumnName("risks")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonSerializerOptions),
                value => JsonSerializer.Deserialize<List<ArchitecturalRiskDto>>(value, JsonSerializerOptions) ?? new List<ArchitecturalRiskDto>())
            .Metadata.SetValueComparer(CreateCollectionComparer<ArchitecturalRiskDto>());

        builder.Property<List<ArchitecturalRecommendationDto>>("_recommendations")
            .HasColumnName("recommendations")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonSerializerOptions),
                value => JsonSerializer.Deserialize<List<ArchitecturalRecommendationDto>>(value, JsonSerializerOptions) ?? new List<ArchitecturalRecommendationDto>())
            .Metadata.SetValueComparer(CreateCollectionComparer<ArchitecturalRecommendationDto>());

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex("AnalysisRequestId_Value").IsUnique();
        builder.HasIndex(x => x.RequestedByUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
    }

    private static ValueComparer<List<T>> CreateCollectionComparer<T>()
    {
        return new ValueComparer<List<T>>(
            (left, right) => JsonSerializer.Serialize(left, JsonSerializerOptions) == JsonSerializer.Serialize(right, JsonSerializerOptions),
            value => JsonSerializer.Serialize(value, JsonSerializerOptions).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(value, JsonSerializerOptions), JsonSerializerOptions) ?? new List<T>());
    }

    private static ValueComparer<IReadOnlyCollection<string>> CreateStringCollectionComparer()
    {
        return new ValueComparer<IReadOnlyCollection<string>>(
            (left, right) => (left ?? Array.Empty<string>()).SequenceEqual(right ?? Array.Empty<string>(), StringComparer.Ordinal),
            value => (value ?? Array.Empty<string>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
            value => (value ?? Array.Empty<string>()).ToArray());
    }
}
