using Shared.Kernel.Primitives;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;

namespace UploadService.Domain.Events;

public class AnalysisRequestCreatedDomainEvent : DomainEvent
{
    public Guid AnalysisRequestId { get; }
    public Guid RequestedByUserId { get; }
    public FileMetadata FileMetadata { get; }
    public FileHash FileHash { get; }
    public StorageObjectKey StorageObjectKey { get; }
    public AnalysisStatus Status { get; }

    public AnalysisRequestCreatedDomainEvent(
        Guid analysisRequestId,
        Guid requestedByUserId,
        FileMetadata fileMetadata,
        FileHash fileHash,
        StorageObjectKey storageObjectKey,
        AnalysisStatus status)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        FileMetadata = fileMetadata;
        FileHash = fileHash;
        StorageObjectKey = storageObjectKey;
        Status = status;
    }
}