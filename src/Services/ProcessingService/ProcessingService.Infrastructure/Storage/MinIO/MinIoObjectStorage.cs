using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Infrastructure.Exceptions;
using System.Diagnostics;

namespace ProcessingService.Infrastructure.Storage.MinIO;

public sealed class MinIoObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minioClient;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<MinIoObjectStorage> _logger;

    public MinIoObjectStorage(IMinioClient minioClient, ActivitySource activitySource, ILogger<MinIoObjectStorage> logger)
    {
        _minioClient = minioClient;
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<StoredObjectContent> DownloadAsync(
        DownloadObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BucketName))
            throw new StorageUnavailableException("Bucket name is required to download the analysis source file.");

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
            throw new StorageUnavailableException("Object key is required to download the analysis source file.");

        using var activity = _activitySource.StartActivity(
            "MinIO download report",
            ActivityKind.Client);

        activity?.SetTag("storage.system", "minio");
        activity?.SetTag("storage.bucket_name", request.BucketName);
        activity?.SetTag("storage.object_key", request.ObjectKey);

        try
        {
            _logger.LogInformation(
                "Downloading object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}",
                request.BucketName,
                request.ObjectKey);

            var stat = await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(request.BucketName)
                    .WithObject(request.ObjectKey),
                cancellationToken);

            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(request.BucketName)
                    .WithObject(request.ObjectKey)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream)),
                cancellationToken);

            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}, Size: {Size} bytes",
                request.BucketName,
                request.ObjectKey,
                stat.Size);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new StoredObjectContent(
                memoryStream,
                stat.ContentType,
                stat.Size,
                stat.ETag);
        }
        catch (Exception ex) when (ex is not StorageUnavailableException)
        {
            _logger.LogError(
                ex,
                "Failed to download object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}",
                request.BucketName,
                request.ObjectKey);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            throw new StorageUnavailableException(
                $"Unable to download object '{request.ObjectKey}' from bucket '{request.BucketName}'.",
                ex);
        }
    }
}
