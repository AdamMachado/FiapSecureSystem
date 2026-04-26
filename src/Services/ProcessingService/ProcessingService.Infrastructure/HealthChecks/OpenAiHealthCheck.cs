using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.HealthChecks;

public sealed class OpenAiHealthCheck : IHealthCheck
{
    private readonly OpenAiOptions _options;

    public OpenAiHealthCheck(IOptions<OpenAiOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return Task.FromResult(HealthCheckResult.Degraded("OpenAI API key is not configured. AI analysis will fail unless a stub analyzer is registered."));

        if (string.IsNullOrWhiteSpace(_options.Model))
            return Task.FromResult(HealthCheckResult.Unhealthy("OpenAI model is not configured."));

        // Avoids a paid API call in health checks. Real connectivity failures are handled by the analyzer itself.
        return Task.FromResult(HealthCheckResult.Healthy("OpenAI configuration is present."));
    }
}
