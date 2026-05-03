using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ReportService.Application.Abstractions.Storage;
using ReportService.Infrastructure.Configuration.Options;
using ReportService.Infrastructure.Exceptions;
using System.Diagnostics;

namespace ReportService.Infrastructure.Storage.MinIO;

public sealed class MinIoReportStorage : IReportStorage
{
    private readonly IMinioClient _minioClient;
    private readonly StorageOptions _storageOptions;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<MinIoReportStorage> _logger;

    public MinIoReportStorage(
        IMinioClient minioClient,
        IOptions<StorageOptions> storageOptions,
        ActivitySource activitySource,
        ILogger<MinIoReportStorage> logger)
    {
        _minioClient = minioClient;
        _storageOptions = storageOptions.Value;
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<StoredReportDescriptor> UploadAsync(
        UploadReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        using var activity = _activitySource.StartActivity(
            "MinIO upload report",
            ActivityKind.Client);

        activity?.SetTag("storage.system", "minio");
        activity?.SetTag("storage.file_name", request.FileName);
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

            var objectKey = GenerateObjectKey(request.FileName);

            using var stream = new MemoryStream(request.Content);

            var putArgs = new PutObjectArgs()
                .WithBucket(_storageOptions.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(request.ContentType);

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new StoredReportDescriptor(
                BucketName: _storageOptions.BucketName,
                ObjectKey: objectKey,
                FileName: request.FileName,
                ContentType: request.ContentType);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            throw new ReportStorageUnavailableException(
                $"Failed to upload report '{request.FileName}' to bucket '{_storageOptions.BucketName}'.",
                ex);
        }
    }

    public async Task<DownloadedReportDescriptor?> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "MinIO download report",
            ActivityKind.Client);

        activity?.SetTag("storage.system", "minio");
        activity?.SetTag("storage.bucket_name", bucketName);
        activity?.SetTag("storage.object_key", objectKey);

        try
        {
            _logger.LogInformation(
                "Downloading object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}",
                bucketName,
                objectKey);

            var stat = await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey),
                cancellationToken);

            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(source => source.CopyTo(memoryStream)),
                cancellationToken);

            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}, Size: {Size} bytes",
                bucketName,
                objectKey,
                stat.Size);

            var fileName = Path.GetFileName(objectKey);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new DownloadedReportDescriptor(
                FileName: fileName,
                ContentType: stat.ContentType ?? "application/octet-stream",
                Content: memoryStream);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to download object from MinIO. Bucket: {BucketName}, ObjectKey: {ObjectKey}",
                bucketName,
                objectKey);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return null;
        }
    }

    private static string GenerateObjectKey(string fileName)
    {
        var safeFileName = string.IsNullOrWhiteSpace(fileName)
            ? "report.json"
            : fileName.Trim().Replace(" ", "_");

        return $"reports/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";
    }
}