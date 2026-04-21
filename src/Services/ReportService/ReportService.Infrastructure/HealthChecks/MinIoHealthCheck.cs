using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ReportService.Infrastructure.Configuration.Options;

namespace ReportService.Infrastructure.HealthChecks;

public sealed class MinIoHealthCheck : IHealthCheck
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _storageOptions;

    public MinIoHealthCheck(IMinioClient client, IOptions<StorageOptions> storageOptions)
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
            await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_storageOptions.BucketName),
                cancellationToken);

            return HealthCheckResult.Healthy("MinIO is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO is unavailable.", ex);
        }
    }
}