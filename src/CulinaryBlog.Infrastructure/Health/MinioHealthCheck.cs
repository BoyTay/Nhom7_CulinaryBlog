using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.Health;

public sealed class MinioHealthCheck(IConfiguration configuration, IHttpClientFactory clientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["Minio:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return HealthCheckResult.Unhealthy("MinIO endpoint is not configured.");
        }

        var baseUrl = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : $"http://{endpoint}";
        if (!Uri.TryCreate($"{baseUrl.TrimEnd('/')}/minio/health/ready", UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https"))
        {
            return HealthCheckResult.Unhealthy("MinIO endpoint is invalid.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await clientFactory.CreateClient("MinioHealth").GetAsync(address, timeout.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("MinIO readiness endpoint responded successfully.")
                : HealthCheckResult.Unhealthy($"MinIO readiness endpoint returned {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("MinIO readiness check failed.", exception);
        }
    }
}
