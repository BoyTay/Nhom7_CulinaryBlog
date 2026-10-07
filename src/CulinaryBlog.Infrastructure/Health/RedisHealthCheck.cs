using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.Health;

public sealed class RedisHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private static readonly byte[] PingCommand = "*1\r\n$4\r\nPING\r\n"u8.ToArray();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(endpoint) ||
            !Uri.TryCreate($"redis://{endpoint}", UriKind.Absolute, out var address) ||
            string.IsNullOrWhiteSpace(address.Host))
        {
            return HealthCheckResult.Unhealthy("Redis endpoint is not configured.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var client = new TcpClient();
            await client.ConnectAsync(address.Host, address.IsDefaultPort ? 6379 : address.Port, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(PingCommand, timeout.Token);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var response = await reader.ReadLineAsync(timeout.Token);
            return response == "+PONG"
                ? HealthCheckResult.Healthy("Redis responded to PING.")
                : HealthCheckResult.Unhealthy("Redis returned an unexpected PING response.");
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis readiness check failed.", exception);
        }
    }
}
