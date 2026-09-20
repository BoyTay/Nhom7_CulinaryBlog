using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.Integration.Tests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();
    }

    [Fact]
    public async Task ApiInformationReturnsVersionedServiceMetadata()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/", UriKind.Relative));
        var payload = await response.Content.ReadFromJsonAsync<ApiInformation>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Culinary Blog API", payload.Name);
        Assert.Equal("v1", payload.Version);
    }

    [Fact]
    public async Task LivenessEndpointDoesNotDependOnExternalServices()
    {
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record ApiInformation(string Name, string Version);
}
