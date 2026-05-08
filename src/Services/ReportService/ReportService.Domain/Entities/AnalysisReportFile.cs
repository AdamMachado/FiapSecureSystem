using ReportService.Domain.Enums;
using Shared.Kernel.Primitives;

namespace ReportService.Domain.Entities;

public sealed class AnalysisReportFile : Entity<Guid>
{
    private AnalysisReportFile()
    {
    }

    private AnalysisReportFile(
        Guid id,
        Guid analysisReportId,
        ReportFormat format,
        string bucketName,
        string objectKey,
        string contentType,
        string fileName,
        DateTime createdAtUtc)
        : base(id)
    {
        if (analysisReportId == Guid.Empty)
            throw new ArgumentException("AnalysisReportId cannot be empty.", nameof(analysisReportId));

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be empty.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be empty.", nameof(objectKey));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        AnalysisReportId = analysisReportId;
        Format = format;
        BucketName = bucketName.Trim();
        ObjectKey = objectKey.Trim();
        ContentType = contentType.Trim();
        FileName = fileName.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid AnalysisReportId { get; private set; }
    public ReportFormat Format { get; private set; }
    public string BucketName { get; private set; } = default!;
    public string ObjectKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public static AnalysisReportFile Create(
        Guid id,
        Guid analysisReportId,
        ReportFormat format,
        string bucketName,
        string objectKey,
        string contentType,
        string fileName,
        DateTime createdAtUtc)
    {
        return new AnalysisReportFile(
            id,
            analysisReportId,
            format,
            bucketName,
            objectKey,
            contentType,
            fileName,
            createdAtUtc);
    }
}
