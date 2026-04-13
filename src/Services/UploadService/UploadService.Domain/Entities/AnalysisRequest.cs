using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;
using UploadService.Domain.Enums;
using UploadService.Domain.Events;
using UploadService.Domain.Exceptions;
using UploadService.Domain.ValueObjects;

namespace UploadService.Domain.Entities;

public sealed class AnalysisRequest : AggregateRoot<Guid>
{
    private const long DefaultMaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    private AnalysisRequest(
        Guid id,
        Guid requestedByUserId,
        FileMetadata fileMetadata,
        FileHash fileHash,
        StorageObjectKey storageObjectKey,
        DateTime createdAtUtc)
        : base(id)
    {
        RequestedByUserId = requestedByUserId;
        FileMetadata = fileMetadata;
        FileHash = fileHash;
        StorageObjectKey = storageObjectKey;
        Status = AnalysisStatus.Received;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    // EF Core
    private AnalysisRequest()
    {
    }

    public Guid RequestedByUserId { get; private set; }
    public FileMetadata FileMetadata { get; private set; } = default!;
    public FileHash FileHash { get; private set; } = default!;
    public StorageObjectKey StorageObjectKey { get; private set; } = default!;
    public AnalysisStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public static AnalysisRequest Create(
        Guid id,
        Guid requestedByUserId,
        FileMetadata fileMetadata,
        FileHash fileHash,
        StorageObjectKey storageObjectKey,
        DateTime createdAtUtc,
        long maxFileSizeInBytes = DefaultMaxFileSizeInBytes)
    {
        ValidateUpload(fileMetadata, maxFileSizeInBytes);

        var entity = new AnalysisRequest(
            id,
            requestedByUserId,
            fileMetadata,
            fileHash,
            storageObjectKey,
            createdAtUtc);

        entity.RaiseDomainEvent(new AnalysisRequestCreatedDomainEvent(
            entity.Id,
            entity.RequestedByUserId,
            entity.FileMetadata,
            entity.FileHash,
            entity.StorageObjectKey,
            entity.Status));

        return entity;
    }

    public void MarkAsProcessing(DateTime updatedAtUtc)
    {
        EnsureTransitionAllowed(AnalysisStatus.Processing);

        var previous = Status;

        Status = AnalysisStatus.Processing;
        StartedAtUtc ??= updatedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        FailureReason = null;

        RaiseDomainEvent(new AnalysisStatusChangedDomainEvent(
            Id,
            previous,
            Status));
    }

    public void MarkAsCompleted(DateTime updatedAtUtc)
    {
        EnsureTransitionAllowed(AnalysisStatus.Completed);

        var previous = Status;

        Status = AnalysisStatus.Completed;
        CompletedAtUtc = updatedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        FailureReason = null;

        RaiseDomainEvent(new AnalysisStatusChangedDomainEvent(
            Id,
            previous,
            Status));
    }

    public void MarkAsFailed(string reason, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason cannot be empty.", nameof(reason));

        EnsureTransitionAllowed(AnalysisStatus.Failed);

        var previous = Status;

        Status = AnalysisStatus.Failed;
        FailedAtUtc = updatedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        FailureReason = reason.Trim();

        RaiseDomainEvent(new AnalysisStatusChangedDomainEvent(
            Id,
            previous,
            Status,
            FailureReason));
    }

    private static void ValidateUpload(FileMetadata fileMetadata, long maxFileSizeInBytes)
    {
        if (fileMetadata.SizeInBytes <= 0)
            throw new EmptyUploadException();

        if (fileMetadata.SizeInBytes > maxFileSizeInBytes)
            throw new FileSizeExceededException(maxFileSizeInBytes, fileMetadata.SizeInBytes);

        if (fileMetadata.FileType is not FileType.Pdf
            && fileMetadata.FileType is not FileType.Png
            && fileMetadata.FileType is not FileType.Jpeg)
        {
            throw new UnsupportedFileTypeException(fileMetadata.ContentType);
        }
    }

    private void EnsureTransitionAllowed(AnalysisStatus targetStatus)
    {
        var isAllowed = (Status, targetStatus) switch
        {
            (AnalysisStatus.Received, AnalysisStatus.Processing) => true,
            (AnalysisStatus.Received, AnalysisStatus.Failed) => true,
            (AnalysisStatus.Processing, AnalysisStatus.Completed) => true,
            (AnalysisStatus.Processing, AnalysisStatus.Failed) => true,
            _ => false
        };

        if (!isAllowed)
        {
            throw new DomainException(
                $"Invalid status transition from '{Status}' to '{targetStatus}'.");
        }
    }
}