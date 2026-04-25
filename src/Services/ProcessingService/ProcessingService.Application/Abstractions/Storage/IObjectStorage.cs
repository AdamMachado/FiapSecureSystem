namespace ProcessingService.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<StoredObjectContent> DownloadAsync(
        DownloadObjectRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DownloadObjectRequest(string BucketName, string ObjectKey);

public sealed record StoredObjectContent(
    Stream Content,
    string ContentType,
    long? SizeInBytes,
    string? ETag = null);
