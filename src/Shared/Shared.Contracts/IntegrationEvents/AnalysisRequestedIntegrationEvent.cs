using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed record AnalysisRequestedIntegrationEvent : IntegrationEventBase
{
    public AnalysisRequestedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid jobId,
        string fileName,
        string contentType,
        string storageBucket,
        string storageObjectKey)
        : base(correlationId, causationId)
    {
        JobId = jobId;
        FileName = fileName;
        ContentType = contentType;
        StorageBucket = storageBucket;
        StorageObjectKey = storageObjectKey;
    }

    public override string EventType => nameof(AnalysisRequestedIntegrationEvent);

    public Guid JobId { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public string StorageBucket { get; init; }
    public string StorageObjectKey { get; init; }
}