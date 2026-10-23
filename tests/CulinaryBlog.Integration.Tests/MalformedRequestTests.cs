using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CulinaryBlog.Integration.Tests;

// CR-001 and Appendix A: syntax and binding errors are client errors (400), never 500.
public sealed class MalformedRequestTests : IClassFixture<CategoryEndpointTests.CategoryFactory>
{
    private static readonly Uri CategoriesUri = new("/api/v1/categories", UriKind.Relative);
    private readonly CategoryEndpointTests.CategoryFactory _factory;

    public MalformedRequestTests(CategoryEndpointTests.CategoryFactory factory) => _factory = factory;

    public static TheoryData<string, byte[]> BadBodies => new()
    {
        { "truncated JSON", Encoding.UTF8.GetBytes("{\"name\":\"Mon an\",") },
        { "not JSON at all", Encoding.UTF8.GetBytes("{bad") },
        { "invalid UTF-8 bytes", [0x7B, 0x22, 0x6E, 0x22, 0x3A, 0xFF, 0xFE, 0x7D] },
        { "wrong value type", Encoding.UTF8.GetBytes("{\"name\":\"Mon an\",\"orderIndex\":\"abc\"}") },
        { "empty body", [] },
    };

    [Theory]
    [MemberData(nameof(BadBodies))]
    public async Task UnreadableBodyReturnsMalformedRequestProblemWithoutDetails(string scenario, byte[] body)
    {
        Assert.False(string.IsNullOrEmpty(scenario));
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await client.PostAsync(CategoriesUri, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        Assert.Equal("MALFORMED_REQUEST", document.RootElement.GetProperty("type").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("Exception", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("at System.", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidBodyStillWorksSoTheHandlerDoesNotSwallowGoodRequests()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        using var content = new StringContent("{\"name\":\"Mon hop le " + Guid.NewGuid().ToString("N")[..6] + "\"}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(CategoriesUri, content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
