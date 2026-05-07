using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using System.Diagnostics;
using UploadService.Application.Abstractions.Storage;
using UploadService.Domain.ValueObjects;
using UploadService.Infrastructure.Configuration.Options;
using UploadService.Infrastructure.Exceptions;

namespace UploadService.Infrastructure.Storage.MinIO;

public sealed class MinIoObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minioClient;
    private readonly StorageOptions _storageOptions;
    private readonly ActivitySource _activitySource;

    public MinIoObjectStorage(
        IMinioClient minioClient,
        IOptions<StorageOptions> storageOptions,
        ActivitySource activitySource)
    {
        _minioClient = minioClient;
        _storageOptions = storageOptions.Value;
        _activitySource = activitySource;
    }

    public async Task<StoredObjectDescriptor> UploadAsync(
        UploadObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        using var activity = _activitySource.StartActivity(
            "MinIO upload object",
            ActivityKind.Client);

        activity?.SetTag("storage.system", "minio");
        activity?.SetTag("storage.object.key", request.ObjectKey);
        activity?.SetTag("storage.content_type", request.ContentType);

        try
        {
            if (_storageOptions.AutoCreateBucket)
            {
                var exists = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_storageOptions.BucketName),
                    cancellationToken);

                if (!exists)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_storageOptions.BucketName),
                        cancellationToken);
                }
            }

            if (request.Content.CanSeek)
                request.Content.Position = 0;

            var putArgs = new PutObjectArgs()
                .WithBucket(_storageOptions.BucketName)
                .WithObject(request.ObjectKey)
                .WithStreamData(request.Content)
                .WithObjectSize(request.Content.CanSeek ? request.Content.Length : -1)
                .WithContentType(request.ContentType);

            var response = await _minioClient.PutObjectAsync(putArgs, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new StoredObjectDescriptor(
                StorageLocation.Create(_storageOptions.BucketName, request.ObjectKey),
                response.Etag);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            throw new StorageUnavailableException(
                $"Failed to upload object '{request.ObjectKey}' to bucket '{_storageOptions.BucketName}'.",
                ex);
        }
    }

    public async Task<StoredObjectContent> DownloadAsync(
        DownloadObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.BucketName))
            throw new StorageUnavailableException("Bucket name is required to download the analysis source file.");

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
            throw new StorageUnavailableException("Object key is required to download the analysis source file.");

        using var activity = _activitySource.StartActivity(
            "MinIO download object",
            ActivityKind.Client);

        activity?.SetTag("storage.system", "minio");
        activity?.SetTag("storage.bucket_name", request.BucketName);
        activity?.SetTag("storage.object_key", request.ObjectKey);

        try
        {
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

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new StoredObjectContent(
                memoryStream,
                stat.ContentType,
                stat.Size,
                stat.ETag);
        }
        catch (Exception ex) when (ex is not StorageUnavailableException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            throw new StorageUnavailableException(
                $"Unable to download object '{request.ObjectKey}' from bucket '{request.BucketName}'.",
                ex);
        }
    }
}
