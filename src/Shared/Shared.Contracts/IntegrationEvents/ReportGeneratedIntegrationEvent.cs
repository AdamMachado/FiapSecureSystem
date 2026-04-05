using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed record ReportGeneratedIntegrationEvent : IntegrationEventBase
{
    public ReportGeneratedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid jobId,
        Guid reportId,
        string storageBucket,
        string storageObjectKey,
        string fileName,
        DateTime generatedAtUtc)
        : base(correlationId, causationId)
    {
        JobId = jobId;
        ReportId = reportId;
        StorageBucket = storageBucket;
        StorageObjectKey = storageObjectKey;
        FileName = fileName;
        GeneratedAtUtc = generatedAtUtc;
    }

    public override string EventType => nameof(ReportGeneratedIntegrationEvent);

    public Guid JobId { get; init; }
    public Guid ReportId { get; init; }
    public string StorageBucket { get; init; }
    public string StorageObjectKey { get; init; }
    public string FileName { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}