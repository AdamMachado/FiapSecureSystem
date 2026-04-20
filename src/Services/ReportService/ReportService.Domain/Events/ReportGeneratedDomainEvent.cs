using Shared.Kernel.Primitives;

namespace ReportService.Domain.Events;

public sealed class ReportGeneratedDomainEvent : DomainEvent
{
    public ReportGeneratedDomainEvent(
        Guid reportId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        string bucketName,
        string objectKey,
        string fileName,
        DateTime generatedAtUtc)
    {
        ReportId = reportId;
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        BucketName = bucketName;
        ObjectKey = objectKey;
        FileName = fileName;
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid ReportId { get; }
    public Guid AnalysisRequestId { get; }
    public Guid RequestedByUserId { get; }
    public string BucketName { get; }
    public string ObjectKey { get; }
    public string FileName { get; }
    public DateTime GeneratedAtUtc { get; }
}