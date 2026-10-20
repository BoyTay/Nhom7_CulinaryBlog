using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Integration.Tests;

public sealed class DegradedHealthTests
{
    private const string SecretPassword = "degraded-health-secret";

    [Fact]
    public async Task LivenessStaysHealthyAndReadinessNamesEveryUnreachableDependency()
    {
        var closedPort = GetClosedPort();
        using var factory = new DependencyFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] =
                $"Host=127.0.0.1;Port={closedPort};Database=none;Username=none;Password={SecretPassword};Timeout=2;Command Timeout=2",
            ["ConnectionStrings:Redis"] = $"127.0.0.1:{closedPort}",
            ["Minio:Endpoint"] = $"127.0.0.1:{closedPort}",
        });
        using var client = factory.CreateClient();

        var live = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("Healthy", (await ReadAsync(live)).Status);

        var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var readyBody = await ReadAsync(ready);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", readyBody.Status);
        Assert.Equal("Unhealthy", readyBody.Checks["database"]);
        Assert.Equal("Unhealthy", readyBody.Checks["redis"]);
        Assert.Equal("Unhealthy", readyBody.Checks["minio"]);
        Assert.DoesNotContain(SecretPassword, readyBody.Raw, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", readyBody.Raw, StringComparison.Ordinal);

        var aggregate = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, aggregate.StatusCode);
    }

    [Theory]
    [InlineData(true, true, true, HttpStatusCode.OK, "Healthy")]
    [InlineData(false, true, true, HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    [InlineData(true, false, true, HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    [InlineData(true, true, false, HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    [InlineData(false, false, true, HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    public async Task ReadinessReportsOnlyTheDependenciesThatAreDown(
        bool databaseUp, bool redisUp, bool minioUp, HttpStatusCode expectedStatus, string expectedOverall)
    {
        using var factory = new DependencyFactory(new Dictionary<string, string?>(), stubs: new Dictionary<string, bool>
        {
            ["database"] = databaseUp,
            ["redis"] = redisUp,
            ["minio"] = minioUp,
        });
        using var client = factory.CreateClient();

        var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var body = await ReadAsync(ready);

        Assert.Equal(expectedStatus, ready.StatusCode);
        Assert.Equal(expectedOverall, body.Status);
        Assert.Equal(databaseUp ? "Healthy" : "Unhealthy", body.Checks["database"]);
        Assert.Equal(redisUp ? "Healthy" : "Unhealthy", body.Checks["redis"]);
        Assert.Equal(minioUp ? "Healthy" : "Unhealthy", body.Checks["minio"]);

        var live = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    [Fact]
    public async Task ReadinessBodyOmitsLivenessCheckAndLivenessBodyOmitsDependencies()
    {
        using var factory = new DependencyFactory(new Dictionary<string, string?>(), stubs: new Dictionary<string, bool>
        {
            ["database"] = true,
            ["redis"] = true,
            ["minio"] = true,
        });
        using var client = factory.CreateClient();

        var live = await ReadAsync(await client.GetAsync(new Uri("/health/live", UriKind.Relative)));
        var ready = await ReadAsync(await client.GetAsync(new Uri("/health/ready", UriKind.Relative)));

        Assert.Equal(["self"], live.Checks.Keys);
        Assert.DoesNotContain("self", ready.Checks.Keys);
        Assert.Contains("database", ready.Checks.Keys);
    }

    private static int GetClosedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<HealthBody> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var checks = document.RootElement.GetProperty("checks").EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("name").GetString()!,
                entry => entry.GetProperty("status").GetString()!);
        return new HealthBody(document.RootElement.GetProperty("status").GetString()!, checks, raw);
    }

    private sealed record HealthBody(string Status, Dictionary<string, string> Checks, string Raw);

    private sealed class DependencyFactory(
        Dictionary<string, string?> settings,
        Dictionary<string, bool>? stubs = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // UseSetting is read by minimal hosting before Program.cs registers services, unlike ConfigureAppConfiguration.
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            if (stubs is null)
            {
                return;
            }

            builder.ConfigureServices(services => services.Configure<HealthCheckServiceOptions>(options =>
            {
                var live = options.Registrations.Where(registration => registration.Tags.Contains("live")).ToList();
                options.Registrations.Clear();
                foreach (var registration in live)
                {
                    options.Registrations.Add(registration);
                }

                foreach (var (name, isUp) in stubs)
                {
                    options.Registrations.Add(new HealthCheckRegistration(
                        name,
                        _ => new StubCheck(isUp),
                        failureStatus: null,
                        tags: ["ready"]));
                }
            }));
        }
    }

    private sealed class StubCheck(bool isUp) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(isUp ? HealthCheckResult.Healthy("up") : HealthCheckResult.Unhealthy("down"));
    }
}
