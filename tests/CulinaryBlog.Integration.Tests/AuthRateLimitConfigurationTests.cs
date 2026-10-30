using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.Integration.Tests;

// The auth limit defaults to 10 requests per minute per IP (NFR-SEC-003). End-to-end browser runs make many
// auth calls from one machine, so the permit count can be raised for a test stack without touching code.
public sealed class AuthRateLimitConfigurationTests
{
    [Fact]
    public async Task ConfiguredPermitLimitReplacesTheDefaultOfTen()
    {
        using var factory = new LimitFactory("3");
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            statuses.Add(await LoginAsync(client));
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(3));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[3]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("many")]
    public async Task InvalidPermitLimitFallsBackToTheDefaultOfTen(string configured)
    {
        using var factory = new LimitFactory(configured);
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 11; attempt++)
        {
            statuses.Add(await LoginAsync(client));
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(10));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    private static async Task<HttpStatusCode> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "nobody@example.test", password = "wrong" });
        return response.StatusCode;
    }

    private sealed class LimitFactory(string permitLimit) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("RateLimiting:AuthPermitLimit", permitLimit);
        }
    }
}
