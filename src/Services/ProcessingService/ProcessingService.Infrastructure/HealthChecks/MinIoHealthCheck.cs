using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ProcessingService.Infrastructure.Configuration.Options;

namespace ProcessingService.Infrastructure.HealthChecks;

public sealed class MinIoHealthCheck : IHealthCheck
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _storageOptions;

    public MinIoHealthCheck(
        IMinioClient client,
        IOptions<StorageOptions> storageOptions)
    {
        _client = client;
        _storageOptions = storageOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_storageOptions.BucketName),
                cancellationToken);

            if (!exists && !_storageOptions.AutoCreateBucket)
                return HealthCheckResult.Degraded($"MinIO is reachable, but bucket '{_storageOptions.BucketName}' does not exist.");

            if (!exists && _storageOptions.AutoCreateBucket)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_storageOptions.BucketName),
                    cancellationToken);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO health check failed.", ex);
        }
    }
}
