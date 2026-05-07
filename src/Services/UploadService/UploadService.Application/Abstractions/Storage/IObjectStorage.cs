using UploadService.Domain.ValueObjects;

namespace UploadService.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<StoredObjectDescriptor> UploadAsync(
        UploadObjectRequest request,
        CancellationToken cancellationToken = default);

    Task<StoredObjectContent> DownloadAsync(
        DownloadObjectRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UploadObjectRequest(
    string ObjectKey,
    Stream Content,
    string ContentType);

public sealed record DownloadObjectRequest(
    string BucketName,
    string ObjectKey);

public sealed record StoredObjectDescriptor(
    StorageLocation Location,
    string ETag);

public sealed record StoredObjectContent(
    Stream Content,
    string ContentType,
    long? SizeInBytes,
    string? ETag = null);
