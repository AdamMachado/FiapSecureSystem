namespace UploadService.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<StoredObjectDescriptor> UploadAsync(
        UploadObjectRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UploadObjectRequest(
    string ObjectKey,
    Stream Content,
    string ContentType);

public sealed record StoredObjectDescriptor(
    StorageLocation Location,
    string ETag);