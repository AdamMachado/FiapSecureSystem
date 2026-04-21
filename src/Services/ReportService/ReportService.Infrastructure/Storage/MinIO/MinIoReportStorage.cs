using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ReportService.Application.Abstractions.Storage;
using ReportService.Infrastructure.Configuration.Options;
using ReportService.Infrastructure.Exceptions;

namespace ReportService.Infrastructure.Storage.MinIO;

public sealed class MinIoReportStorage : IReportStorage
{
    private readonly IMinioClient _minioClient;
    private readonly StorageOptions _storageOptions;

    public MinIoReportStorage(
        IMinioClient minioClient,
        IOptions<StorageOptions> storageOptions)
    {
        _minioClient = minioClient;
        _storageOptions = storageOptions.Value;
    }

    public async Task<StoredReportDescriptor> UploadAsync(
        UploadReportRequest request,
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

            var objectKey = GenerateObjectKey(request.FileName);

            using var stream = new MemoryStream(request.Content);

            var putArgs = new PutObjectArgs()
                .WithBucket(_storageOptions.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(request.ContentType);

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);

            return new StoredReportDescriptor(
                BucketName: _storageOptions.BucketName,
                ObjectKey: objectKey,
                FileName: request.FileName,
                ContentType: request.ContentType);
        }
        catch (Exception ex)
        {
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
        try
        {
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

            var fileName = Path.GetFileName(objectKey);

            return new DownloadedReportDescriptor(
                FileName: fileName,
                ContentType: stat.ContentType ?? "application/octet-stream",
                Content: memoryStream);
        }
        catch
        {
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