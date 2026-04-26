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

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AnalysisRequestId)
            .HasConversion(
                value => value.Value,
                value => AnalysisRequestId.Create(value))
            .HasColumnName("analysis_request_id")
            .IsRequired()
            .Metadata.SetValueComparer(CreateAnalysisRequestIdComparer());

        builder.Property(x => x.RequestedByUserId)
            .IsRequired();

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
                    warnings => JsonSerializer.Serialize(warnings, JsonSerializerOptions),
                    json => JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions) ?? new List<string>())
                .Metadata.SetValueComparer(CreateStringCollectionComparer());
        });

        builder.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        builder.Property(x => x.FailureDetails)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.FailedAtUtc);

        builder.Ignore(x => x.Components);
        builder.Ignore(x => x.Risks);
        builder.Ignore(x => x.Recommendations);

        builder.Property<string>("_componentsJson")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("components")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<string>("_risksJson")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("risks")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<string>("_recommendationsJson")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("recommendations")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.AnalysisRequestId)
            .HasDatabaseName("ix_analysis_processes_analysis_request_id");

        builder.HasIndex(x => x.RequestedByUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
    }

    private static ValueComparer<AnalysisRequestId> CreateAnalysisRequestIdComparer()
    {
        return new ValueComparer<AnalysisRequestId>(
            (left, right) => left != null && right != null && left.Value == right.Value,
            value => value.Value.GetHashCode(),
            value => AnalysisRequestId.Create(value.Value));
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
