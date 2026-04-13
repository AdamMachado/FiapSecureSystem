using Shared.Contracts.IntegrationEvents.Abstractions;

namespace Shared.Contracts.IntegrationEvents;

public sealed class ReportGeneratedIntegrationEvent : IntegrationEventBase
{
    public ReportGeneratedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        Guid reportId,
        string storageBucket,
        string storageObjectKey,
        string fileName,
        DateTime generatedAtUtc)
        : base(correlationId, causationId)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        ReportId = reportId;
        StorageBucket = storageBucket;
        StorageObjectKey = storageObjectKey;
        FileName = fileName;
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public Guid ReportId { get; init; }
    public string StorageBucket { get; init; }
    public string StorageObjectKey { get; init; }
    public string FileName { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}