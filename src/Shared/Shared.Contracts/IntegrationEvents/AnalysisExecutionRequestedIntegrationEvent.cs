using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed class AnalysisExecutionRequestedIntegrationEvent : IntegrationEventBase
{
    public AnalysisExecutionRequestedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisProcessId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        string fileName,
        string contentType,
        string fileHash,
        string storageBucket,
        string storageObjectKey)
        : base(correlationId, causationId)
    {
        AnalysisProcessId = analysisProcessId;
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        FileName = fileName;
        ContentType = contentType;
        FileHash = fileHash;
        StorageBucket = storageBucket;
        StorageObjectKey = storageObjectKey;
    }

    public Guid AnalysisProcessId { get; init; }
    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public string FileHash { get; init; }
    public string StorageBucket { get; init; }
    public string StorageObjectKey { get; init; }
}
