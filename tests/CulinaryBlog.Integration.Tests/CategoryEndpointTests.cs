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

    public sealed class CategoryFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ICategoryRepository>(new TestCategoryRepository());
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

    private sealed class TestCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [Category.Create("Món chính", "mon-chinh")];

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
            Task.FromResult(0);

        public Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
            Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CategoryRecipeSummary>, int)>(([], 0));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_categories.Where(category => !category.IsDeleted).ToList());

        public void Add(Category category) => _categories.Add(category);
    }
}
