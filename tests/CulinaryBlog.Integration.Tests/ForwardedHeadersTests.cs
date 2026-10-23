using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.Integration.Tests;

// Behind Nginx every request arrives from the proxy's address. Without forwarded-header handling the
// per-IP auth rate limit becomes one shared bucket for all users and refresh tokens record the proxy IP.
public sealed class ForwardedHeadersTests
{
    private static readonly Uri LoginUri = new("/api/v1/auth/login", UriKind.Relative);

    [Fact]
    public async Task RateLimitIsPerClientWhenTheProxyIsTrusted()
    {
        using var factory = new ForwardedFactory(trustProxy: true);
        using var client = factory.CreateClient();

        var firstClient = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 11; attempt++)
        {
            firstClient.Add(await LoginAsync(client, "203.0.113.10"));
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, firstClient.Take(10));
        Assert.Equal(HttpStatusCode.TooManyRequests, firstClient[10]);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginAsync(client, "203.0.113.11"));
    }

    [Fact]
    public async Task ForwardedAddressesAreIgnoredByDefaultSoClientsCannotSpoofTheirIp()
    {
        using var factory = new ForwardedFactory(trustProxy: false);
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 11; attempt++)
        {
            statuses.Add(await LoginAsync(client, $"198.51.100.{attempt + 1}"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    private static async Task<HttpStatusCode> LoginAsync(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, LoginUri)
        {
            Content = JsonContent.Create(new { email = "nobody@example.test", password = "wrong" }),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private sealed class ForwardedFactory(bool trustProxy) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ForwardedHeaders:TrustAllProxies", trustProxy ? "true" : "false");
        }
    }
}
