using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
using CulinaryBlog.Domain.Recipes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeEndpointTests : IClassFixture<RecipeEndpointTests.RecipeFactory>
{
    private static readonly Guid FirstCategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SecondCategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly RecipeFactory _factory;

    public RecipeEndpointTests(RecipeFactory factory) => _factory = factory;

    [Fact]
    public async Task AnonymousListReturnsOnlyPublishedRecipesAndHonorsFilters()
    {
        var client = _factory.CreateClient();

        var filtered = await client.GetFromJsonAsync<RecipeListItemDto[]>(
            $"/api/v1/recipes?categoryId={FirstCategoryId}&difficulty=Easy");
        var drafts = await client.GetFromJsonAsync<RecipeListItemDto[]>(
            "/api/v1/recipes?status=Draft");

        Assert.NotNull(filtered);
        Assert.Contains(filtered, recipe => recipe.Title == "Published easy recipe");
        Assert.DoesNotContain(filtered, recipe => recipe.Title == "Published hard recipe");
        Assert.DoesNotContain(filtered, recipe => recipe.Title == "Owner draft recipe");
        Assert.NotNull(drafts);
        Assert.Empty(drafts);
    }

    [Fact]
    public async Task AuthorListIncludesOwnUnpublishedRecipesButNotOtherAuthorsRecipes()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/recipes");
        request.Headers.Add("X-Test-Role", "Author");
        request.Headers.Add("X-Test-User", "recipe-owner");

        var response = await _factory.CreateClient().SendAsync(request);
        var recipes = await response.Content.ReadFromJsonAsync<RecipeListItemDto[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(recipes);
        Assert.Contains(recipes, recipe => recipe.Title == "Owner draft recipe");
        Assert.DoesNotContain(recipes, recipe => recipe.Title == "Other author draft recipe");
    }

    [Fact]
    public async Task AdminListIncludesUnpublishedRecipesFromAllAuthors()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/recipes");
        request.Headers.Add("X-Test-Role", "Admin");
        request.Headers.Add("X-Test-User", "site-admin");

        var response = await _factory.CreateClient().SendAsync(request);
        var recipes = await response.Content.ReadFromJsonAsync<RecipeListItemDto[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(recipes);
        Assert.Contains(recipes, recipe => recipe.Title == "Owner draft recipe");
        Assert.Contains(recipes, recipe => recipe.Title == "Other author draft recipe");
    }

    [Fact]
    public async Task DetailHidesUnpublishedRecipesFromAnonymousAndOtherAuthors()
    {
        var client = _factory.CreateClient();
        var draftId = _factory.Repository.OwnerDraftId;

        var anonymous = await client.GetAsync($"/api/v1/recipes/{draftId}");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/{draftId}");
        request.Headers.Add("X-Test-Role", "Author");
        request.Headers.Add("X-Test-User", "different-owner");
        var otherAuthor = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherAuthor.StatusCode);
    }

    [Fact]
    public async Task OwnerAndAdminCanReadUnpublishedRecipe()
    {
        var draftId = _factory.Repository.OwnerDraftId;
        using var ownerRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/{draftId}");
        ownerRequest.Headers.Add("X-Test-Role", "Author");
        ownerRequest.Headers.Add("X-Test-User", "recipe-owner");
        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/{draftId}");
        adminRequest.Headers.Add("X-Test-Role", "Admin");
        adminRequest.Headers.Add("X-Test-User", "site-admin");

        var ownerResponse = await _factory.CreateClient().SendAsync(ownerRequest);
        var adminResponse = await _factory.CreateClient().SendAsync(adminRequest);

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRequiresAuthorRoleAndUsesAuthenticatedUserAsOwner()
    {
        var requestBody = new CreateRecipeRequest(
            "New test recipe",
            $"new-test-recipe-{Guid.NewGuid():N}",
            "A recipe created in an integration test.",
            FirstCategoryId,
            5,
            15,
            2,
            RecipeDifficulty.Easy);
        var client = _factory.CreateClient();

        var anonymous = await client.PostAsJsonAsync("/api/v1/recipes", requestBody);
        using var readerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recipes")
        {
            Content = JsonContent.Create(requestBody),
        };
        readerRequest.Headers.Add("X-Test-Role", "Reader");
        var reader = await client.SendAsync(readerRequest);
        using var authorRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recipes")
        {
            Content = JsonContent.Create(requestBody),
        };
        authorRequest.Headers.Add("X-Test-Role", "Author");
        authorRequest.Headers.Add("X-Test-User", "recipe-owner");
        var author = await client.SendAsync(authorRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reader.StatusCode);
        Assert.Equal(HttpStatusCode.Created, author.StatusCode);

        using var created = System.Text.Json.JsonDocument.Parse(await author.Content.ReadAsStringAsync());
        var createdId = created.RootElement.GetProperty("id").GetGuid();
        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/recipes/{createdId}");
        detailRequest.Headers.Add("X-Test-Role", "Author");
        detailRequest.Headers.Add("X-Test-User", "recipe-owner");
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("recipe-owner", detail.AuthorId);
    }

    public sealed class RecipeFactory : WebApplicationFactory<Program>
    {
        private readonly TestRecipeRepository _repository = new();

        internal TestRecipeRepository Repository => _repository;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRecipeRepository>();
                services.AddSingleton<IRecipeRepository>(_repository);
                services.RemoveAll<IDataSession>();
                services.AddSingleton<IDataSession>(new TestSession());
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "RecipeTest";
                    options.DefaultChallengeScheme = "RecipeTest";
                }).AddScheme<AuthenticationSchemeOptions, RecipeTestAuthHandler>("RecipeTest", _ => { });
            });
        }
    }

    private sealed class RecipeTestAuthHandler(
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

            var userId = Request.Headers["X-Test-User"].FirstOrDefault() ?? "recipe-owner";
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role.ToString())],
                Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class TestSession : IDataSession
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    internal sealed class TestRecipeRepository : IRecipeRepository
    {
        private readonly ConcurrentDictionary<Guid, Recipe> _recipes = new();

        public TestRecipeRepository()
        {
            Add(CreateRecipe("Published easy recipe", FirstCategoryId, "recipe-owner", RecipeDifficulty.Easy, RecipeStatus.Published));
            Add(CreateRecipe("Published hard recipe", SecondCategoryId, "another-author", RecipeDifficulty.Hard, RecipeStatus.Published));
            var ownerDraft = CreateRecipe("Owner draft recipe", FirstCategoryId, "recipe-owner", RecipeDifficulty.Easy, RecipeStatus.Draft);
            OwnerDraftId = ownerDraft.Id;
            Add(ownerDraft);
            Add(CreateRecipe("Other author draft recipe", FirstCategoryId, "another-author", RecipeDifficulty.Easy, RecipeStatus.Draft));
        }

        public Guid OwnerDraftId { get; }

        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_recipes.GetValueOrDefault(id));

        public Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_recipes.Values.SingleOrDefault(recipe => recipe.Slug == slug));

        public Task<IReadOnlyList<Recipe>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>(_recipes.Values
                .Where(recipe => recipe.Status == RecipeStatus.Published)
                .ToList());

        public Task<IReadOnlyList<Recipe>> ListAsync(
            Guid? categoryId = null,
            RecipeDifficulty? difficulty = null,
            RecipeStatus? status = null,
            string? authorId = null,
            bool includeAllStatuses = false,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Recipe> query = _recipes.Values;
            if (!includeAllStatuses)
            {
                query = query.Where(recipe => recipe.Status == RecipeStatus.Published
                    || (authorId != null && recipe.AuthorId == authorId));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(recipe => recipe.CategoryId == categoryId.Value);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(recipe => recipe.Difficulty == difficulty.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(recipe => recipe.Status == status.Value);
            }

            return Task.FromResult<IReadOnlyList<Recipe>>(query.ToList());
        }

        public void Add(Recipe recipe) => _recipes[recipe.Id] = recipe;

        public void SetOriginalVersion(Recipe recipe, uint version)
        {
        }

        private static Recipe CreateRecipe(
            string title,
            Guid categoryId,
            string authorId,
            RecipeDifficulty difficulty,
            RecipeStatus status)
        {
            var recipe = Recipe.Create(
                title,
                title.ToLowerInvariant().Replace(' ', '-'),
                "Recipe description.",
                categoryId,
                authorId,
                5,
                15,
                2,
                difficulty);

            if (status == RecipeStatus.Published)
            {
                recipe.AddStep("Cook the recipe.");
                recipe.AddIngredient("Ingredient", 1, "piece");
                recipe.Publish();
            }

            return recipe;
        }
    }
}