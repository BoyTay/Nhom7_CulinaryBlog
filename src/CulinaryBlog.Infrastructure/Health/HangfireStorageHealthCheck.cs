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
            var active = storage.GetMonitoringApi().Servers().Any(server =>
                server.Heartbeat > DateTime.UtcNow.AddMinutes(-2) && server.WorkersCount > 0);
            return Task.FromResult(active
                ? HealthCheckResult.Healthy("Hangfire storage and worker are available.")
                : HealthCheckResult.Unhealthy("No active Hangfire worker."));
        }
        catch (Exception)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Hangfire storage health check failed."));
        }
    }
}
