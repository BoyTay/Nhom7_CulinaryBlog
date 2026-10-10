using System.Net;
using System.Net.Sockets;
using CulinaryBlog.Infrastructure.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Integration.Tests;

public sealed class ReadinessCheckTests
{
    [Fact]
    public async Task ReadinessEndpointIsUnavailableWhenRedisAndMinioAreNotConfigured()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task RedisCheckRequiresPongAndReportsMissingConfiguration()
    {
        var missing = new RedisHealthCheck(Configuration());
        Assert.Equal(HealthStatus.Unhealthy, (await missing.CheckHealthAsync(new HealthCheckContext())).Status);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var buffer = new byte[14];
            await stream.ReadExactlyAsync(buffer);
            await stream.WriteAsync("+PONG\r\n"u8.ToArray());
        });

        var check = new RedisHealthCheck(Configuration(("ConnectionStrings:Redis", $"127.0.0.1:{port}")));
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext())).Status);
        await server;
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HealthStatus.Unhealthy)]
    public async Task MinioCheckUsesReadinessEndpoint(HttpStatusCode statusCode, HealthStatus expected)
    {
        var handler = new RecordingHandler(statusCode);
        var check = new MinioHealthCheck(
            Configuration(("Minio:Endpoint", "minio.local:9000")),
            new SingleClientFactory(new HttpClient(handler)));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(expected, result.Status);
        Assert.Equal("http://minio.local:9000/minio/health/ready", handler.RequestUri?.ToString());
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(value =>
            new KeyValuePair<string, string?>(value.Key, value.Value))).Build();

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
