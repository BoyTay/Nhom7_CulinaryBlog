using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Domain.Categories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Integration.Tests;

public sealed class CategoryEndpointTests : IClassFixture<CategoryEndpointTests.CategoryFactory>
{
    private readonly CategoryFactory _factory;

    public CategoryEndpointTests(CategoryFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicListReturnsCategoryAndMissingDetailReturnsProblem()
    {
        var client = _factory.CreateClient();
        var list = await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));
        var categories = await list.Content.ReadFromJsonAsync<CategoryDto[]>();
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.NotNull(categories);
        Assert.Contains(categories, category => category.Slug == "mon-chinh");

        var missing = await client.GetAsync(new Uri("/api/v1/categories/khong-ton-tai", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateRequiresAdminAndRejectsDuplicateName()
    {
        var client = _factory.CreateClient();
        var body = new CategoryRequest("Món chính", null, null);
        var denied = await client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative), body);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        client.DefaultRequestHeaders.Add("X-Test-Role", "Author");
        var forbidden = await client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative), body);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        var duplicate = await client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative), body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var created = await client.PostAsJsonAsync(
            new Uri("/api/v1/categories", UriKind.Relative),
            new CategoryRequest("Món phụ", null, null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/v1/categories/mon-phu", created.Headers.Location?.ToString());
    }

    [Fact]
    public async Task DetailReturnsEmptyPageAndRejectsInvalidPagination()
    {
        var client = _factory.CreateClient();
        var detail = await client.GetAsync(new Uri("/api/v1/categories/mon-chinh?page=1&pageSize=12", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await detail.Content.ReadAsStreamAsync());
        Assert.Equal("mon-chinh", document.RootElement.GetProperty("category").GetProperty("slug").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("recipes").GetProperty("totalCount").GetInt32());

        var invalid = await client.GetAsync(new Uri("/api/v1/categories/mon-chinh?page=0&pageSize=51", UriKind.Relative));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocumentsCategoryResponsesAndErrorCodes()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/categories").GetProperty("post").GetProperty("responses").TryGetProperty("409", out _));
        Assert.Contains("CATEGORY_NAME_EXISTS", paths.GetProperty("/api/v1/categories").GetProperty("post").GetProperty("description").GetString());
        Assert.True(paths.GetProperty("/api/v1/categories/{id}").GetProperty("delete").GetProperty("responses").TryGetProperty("204", out _));
        Assert.Contains("CATEGORY_DELETE_HAS_RECIPES", paths.GetProperty("/api/v1/categories/{id}").GetProperty("delete").GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdateRequiresAdminPreservesSlugAndRefreshesList()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        var originalName = $"Danh mục {Guid.NewGuid():N}";
        var created = await client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative),
            new CategoryRequest(originalName, null, null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative)); // prime list cache

        var updatedName = $"Đã sửa {Guid.NewGuid():N}";
        var updated = await client.PutAsJsonAsync(new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative),
            new CategoryRequest(updatedName, "Mô tả mới", null, 3));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var result = await updated.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(result);
        Assert.Equal(category.Slug, result.Slug);
        Assert.Equal(updatedName, result.Name);

        var list = await client.GetFromJsonAsync<CategoryDto[]>(new Uri("/api/v1/categories", UriKind.Relative));
        Assert.NotNull(list);
        Assert.Contains(list, item => item.Id == category.Id && item.Name == updatedName);

        var duplicate = await client.PutAsJsonAsync(new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative),
            new CategoryRequest("Món chính", null, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task DeleteRejectsNonemptyCategoryThenSoftDeletesAndRefreshesList()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        var created = await client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative),
            new CategoryRequest($"Cần xóa {Guid.NewGuid():N}", null, null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));

        _factory.Repository.SetRecipeCount(category.Id, 1);
        var blocked = await client.DeleteAsync(new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        _factory.Repository.SetRecipeCount(category.Id, 0);

        var deleted = await client.DeleteAsync(new Uri($"/api/v1/categories/{category.Id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var list = await client.GetFromJsonAsync<CategoryDto[]>(new Uri("/api/v1/categories", UriKind.Relative));
        Assert.NotNull(list);
        Assert.DoesNotContain(list, item => item.Id == category.Id);
        var missing = await client.GetAsync(new Uri($"/api/v1/categories/{category.Slug}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    public sealed class CategoryFactory : WebApplicationFactory<Program>
    {
        private readonly TestCategoryRepository _repository = new();
        internal TestCategoryRepository Repository => _repository;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ICategoryRepository>(_repository);
                services.AddSingleton<IDataSession>(new TestSession());
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "test-admin"), new Claim(ClaimTypes.Role, role.ToString())],
                Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class TestSession : IDataSession
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    internal sealed class TestCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [Category.Create("Món chính", "mon-chinh")];
        private readonly Dictionary<Guid, int> _recipeCounts = [];

        public void SetRecipeCount(Guid categoryId, int count) => _recipeCounts[categoryId] = count;

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.SingleOrDefault(category => category.Id == id && !category.IsDeleted));

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.SingleOrDefault(category => category.Slug == slug && !category.IsDeleted));

        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.Any(category => category.Id != excludeId &&
                string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.Any(category => category.Slug == slug));

        public Task<int> CountRecipesAsync(Guid categoryId, bool publishedOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult(_recipeCounts.GetValueOrDefault(categoryId));

        public Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
            Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CategoryRecipeSummary>, int)>(([], 0));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_categories.Where(category => !category.IsDeleted).ToList());

        public void Add(Category category) => _categories.Add(category);
    }
}
