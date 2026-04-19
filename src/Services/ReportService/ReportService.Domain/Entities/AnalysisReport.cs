using ReportService.Domain.Enums;
using ReportService.Domain.Events;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ReportService.Domain.Entities;

public sealed class AnalysisReport : AggregateRoot<Guid>
{
    private AnalysisReport()
    {
    }

    private AnalysisReport(
        Guid id,
        Guid analysisRequestId,
        Guid requestedByUserId,
        ReportFormat format,
        string content,
        string bucketName,
        string objectKey,
        string fileName,
        string contentType,
        DateTime createdAtUtc)
        : base(id)
    {
        if (analysisRequestId == Guid.Empty)
            throw new ArgumentException("AnalysisRequestId cannot be empty.", nameof(analysisRequestId));

        if (requestedByUserId == Guid.Empty)
            throw new ArgumentException("RequestedByUserId cannot be empty.", nameof(requestedByUserId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Report content cannot be empty.", nameof(content));

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be empty.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be empty.", nameof(objectKey));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        Format = format;
        Status = ReportStatus.Generated;
        Content = content;
        GeneratedFileLocation = new GeneratedFileLocation(bucketName, objectKey);
        FileName = fileName;
        ContentType = contentType;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        GeneratedAtUtc = createdAtUtc;
    }

    public Guid AnalysisRequestId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public ReportFormat Format { get; private set; }
    public ReportStatus Status { get; private set; }
    public string Content { get; private set; } = default!;
    public GeneratedFileLocation GeneratedFileLocation { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? GeneratedAtUtc { get; private set; }

    public static AnalysisReport Create(
        Guid id,
        Guid analysisRequestId,
        Guid requestedByUserId,
        ReportFormat format,
        string content,
        string bucketName,
        string objectKey,
        string fileName,
        string contentType,
        DateTime createdAtUtc)
    {
        var report = new AnalysisReport(
            id,
            analysisRequestId,
            requestedByUserId,
            format,
            content,
            bucketName,
            objectKey,
            fileName,
            contentType,
            createdAtUtc);

        report.RaiseDomainEvent(new ReportGeneratedDomainEvent(
            report.Id,
            report.AnalysisRequestId,
            report.RequestedByUserId,
            report.GeneratedFileLocation.BucketName,
            report.GeneratedFileLocation.ObjectKey,
            report.FileName,
            report.GeneratedAtUtc!.Value));

        return report;
    }

    public void MarkAsGenerated(DateTime updatedAtUtc)
    {
        if (Status == ReportStatus.Generated)
            return;

        if (Status == ReportStatus.Failed)
            throw new DomainException("Cannot transition a failed report to generated.");

        Status = ReportStatus.Generated;
        UpdatedAtUtc = updatedAtUtc;
        GeneratedAtUtc = updatedAtUtc;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason cannot be empty.", nameof(reason));

        if (Status == ReportStatus.Generated)
            throw new DomainException("Cannot fail a report that has already been generated.");

        Status = ReportStatus.Failed;
        UpdatedAtUtc = updatedAtUtc;
        FailureReason = reason.Trim();
    }
}

public sealed record GeneratedFileLocation(string BucketName, string ObjectKey);