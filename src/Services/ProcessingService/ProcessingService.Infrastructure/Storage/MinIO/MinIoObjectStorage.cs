using Minio;
using Minio.DataModel.Args;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Infrastructure.Exceptions;

namespace ProcessingService.Infrastructure.Storage.MinIO;

public sealed class MinIoObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minioClient;

    public MinIoObjectStorage(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<StoredObjectContent> DownloadAsync(
        DownloadObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BucketName))
            throw new StorageUnavailableException("Bucket name is required to download the analysis source file.");

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
            throw new StorageUnavailableException("Object key is required to download the analysis source file.");

        try
        {
            var stat = await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(request.BucketName)
                    .WithObject(request.ObjectKey),
                cancellationToken);

            var memory = new MemoryStream();

            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(request.BucketName)
                    .WithObject(request.ObjectKey)
                    .WithCallbackStream(stream => stream.CopyTo(memory)),
                cancellationToken);

            memory.Position = 0;

            return new StoredObjectContent(
                memory,
                stat.ContentType,
                stat.Size,
                stat.ETag);
        }
        catch (Exception ex) when (ex is not StorageUnavailableException)
        {
            throw new StorageUnavailableException(
                $"Unable to download object '{request.ObjectKey}' from bucket '{request.BucketName}'.",
                ex);
        }
    }
}
