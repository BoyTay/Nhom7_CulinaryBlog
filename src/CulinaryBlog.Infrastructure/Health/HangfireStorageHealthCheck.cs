using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.Health;

public sealed class HangfireStorageHealthCheck(JobStorage storage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = storage.GetMonitoringApi().Servers();
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire PostgreSQL storage is reachable."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Hangfire storage health check failed.", exception));
        }
    }
}
