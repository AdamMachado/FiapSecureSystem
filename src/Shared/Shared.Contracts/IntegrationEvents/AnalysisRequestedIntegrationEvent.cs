using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed class AnalysisRequestedIntegrationEvent : IntegrationEventBase
{
    public AnalysisRequestedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        string fileName,
        string contentType,
        string fileHash,
        string storageBucket,
        string storageObjectKey)
        : base(correlationId, causationId)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        FileName = fileName;
        ContentType = contentType;
        FileHash = fileHash;
        StorageBucket = storageBucket;
        StorageObjectKey = storageObjectKey;
    }

    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public string FileHash { get; init; }
    public string StorageBucket { get; init; }
    public string StorageObjectKey { get; init; }
}