using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Categories;

namespace CulinaryBlog.Integration.Tests;

public sealed class CategoryEndpointErrorTests : IClassFixture<CategoryEndpointTests.CategoryFactory>
{
    private static readonly Uri CategoriesUri = new("/api/v1/categories", UriKind.Relative);
    private readonly CategoryEndpointTests.CategoryFactory _factory;

    public CategoryEndpointErrorTests(CategoryEndpointTests.CategoryFactory factory) => _factory = factory;

    public static TheoryData<string, string?, string?, int> InvalidBodies => new()
    {
        { "A", null, null, 0 },
        { "<b>Món</b>", null, null, 0 },
        { "   ", null, null, 0 },
        { "Món hợp lệ", new string('m', 2001), null, 0 },
        { "Món hợp lệ", null, new string('u', 501), 0 },
        { "Món hợp lệ", null, null, -1 },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task CreateRejectsInvalidBodyWithValidationProblem(
        string name, string? description, string? imageUrl, int orderIndex)
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync(
            CategoriesUri, new CategoryRequest(name, description, imageUrl, orderIndex));

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task UpdateRejectsInvalidBodyAndUnknownIdAndMissingRole()
    {
        var client = AdminClient();
        var created = await client.PostAsJsonAsync(CategoriesUri, new CategoryRequest($"Sửa lỗi {Guid.NewGuid():N}", null, null));
        var category = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        var itemUri = new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative);

        var invalid = await client.PutAsJsonAsync(itemUri, new CategoryRequest("A", null, null));
        await AssertProblemAsync(invalid, HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR");

        var missing = await client.PutAsJsonAsync(
            new Uri($"/api/v1/categories/{Guid.NewGuid()}", UriKind.Relative),
            new CategoryRequest("Không tồn tại", null, null));
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "CATEGORY_NOT_FOUND");

        var anonymous = _factory.CreateClient();
        var unauthorized = await anonymous.PutAsJsonAsync(itemUri, new CategoryRequest("Ẩn danh", null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var author = RoleClient("Author");
        var forbidden = await author.PutAsJsonAsync(itemUri, new CategoryRequest("Tác giả", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var unchanged = await client.GetFromJsonAsync<CategoryDto[]>(CategoriesUri);
        Assert.Contains(unchanged!, item => item.Id == category.Id && item.Name == category.Name);
    }

    [Fact]
    public async Task DeleteReportsMissingUnauthenticatedAndForbiddenWithoutChangingData()
    {
        var admin = AdminClient();
        var created = await admin.PostAsJsonAsync(CategoriesUri, new CategoryRequest($"Giữ lại {Guid.NewGuid():N}", null, null));
        var category = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        var itemUri = new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative);

        var missing = await admin.DeleteAsync(new Uri($"/api/v1/categories/{Guid.NewGuid()}", UriKind.Relative));
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "CATEGORY_NOT_FOUND");

        var anonymous = await _factory.CreateClient().DeleteAsync(itemUri);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var forbidden = await RoleClient("Author").DeleteAsync(itemUri);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var stillThere = await admin.GetFromJsonAsync<CategoryDto[]>(CategoriesUri);
        Assert.Contains(stillThere!, item => item.Id == category.Id);
    }

    [Fact]
    public async Task ConflictResponsesCarryBusinessErrorCodeInProblemType()
    {
        var client = AdminClient();
        var name = $"Trùng {Guid.NewGuid():N}";
        await client.PostAsJsonAsync(CategoriesUri, new CategoryRequest(name, null, null));

        var duplicate = await client.PostAsJsonAsync(CategoriesUri, new CategoryRequest(name.ToUpperInvariant(), null, null));

        await AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "CATEGORY_NAME_EXISTS");
    }

    [Fact]
    public async Task CategoryDetailForUnknownSlugIsNotFoundProblemAndDoesNotRequireAuthentication()
    {
        var response = await _factory.CreateClient()
            .GetAsync(new Uri("/api/v1/categories/khong-co-slug-nay", UriKind.Relative));

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "CATEGORY_NOT_FOUND");
    }

    private HttpClient AdminClient() => RoleClient("Admin");

    private HttpClient RoleClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(code, document.RootElement.GetProperty("type").GetString());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
