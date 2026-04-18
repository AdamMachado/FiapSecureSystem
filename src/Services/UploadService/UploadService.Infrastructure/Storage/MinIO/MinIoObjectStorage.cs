using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using UploadService.Application.Abstractions.Storage;
using UploadService.Domain.ValueObjects;
using UploadService.Infrastructure.Configuration.Options;
using UploadService.Infrastructure.Exceptions;

namespace UploadService.Infrastructure.Storage.MinIO;

public sealed class MinIoObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minioClient;
    private readonly StorageOptions _storageOptions;

    public MinIoObjectStorage(
        IMinioClient minioClient,
        IOptions<StorageOptions> storageOptions)
    {
        _minioClient = minioClient;
        _storageOptions = storageOptions.Value;
    }

    public async Task<StoredObjectDescriptor> UploadAsync(
        UploadObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

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

            return new StoredObjectDescriptor(
                StorageLocation.Create(_storageOptions.BucketName, request.ObjectKey),
                response.Etag);
        }
        catch (Exception ex)
        {
            throw new StorageUnavailableException(
                $"Failed to upload object '{request.ObjectKey}' to bucket '{_storageOptions.BucketName}'.",
                ex);
        }
    }
}
