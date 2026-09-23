using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.Integration.Tests;

public sealed class ApiSmokeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(TestWebApplicationFactory factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task ApiInformationReturnsVersionedServiceMetadata()
    {
        var response = await _client.GetAsync(
            new Uri("/api/v1/", UriKind.Relative));

        var payload = await response.Content
            .ReadFromJsonAsync<ApiInformation>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Culinary Blog API", payload.Name);
        Assert.Equal("v1", payload.Version);
    }

    [Fact]
    public async Task LivenessEndpointDoesNotDependOnExternalServices()
    {
        var response = await _client.GetAsync(
            new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipeAsAuthorReturnsCreated()
    {
        var request = new
        {
            Title = "Integration Test Recipe",
            Slug = "integration-test-recipe",
            Description = "Recipe created by integration test.",
            CategoryId = Guid.NewGuid(),
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 2,
            Difficulty = 0
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            request);

        var payload = await response.Content
            .ReadFromJsonAsync<CreateRecipeResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Id);
    }

    [Fact]
    public async Task GetRecipeDetailReturnsCreatedRecipe()
    {
        var request = new
        {
            Title = "Integration Detail Recipe",
            Slug = "integration-detail-recipe",
            Description = "Recipe used to test recipe detail endpoint.",
            CategoryId = Guid.NewGuid(),
            PrepTimeMinutes = 15,
            CookTimeMinutes = 25,
            Servings = 4,
            Difficulty = 1
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            request);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateRecipeResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);

        var detailResponse = await _client.GetAsync(
            $"/api/v1/recipes/{created.Id}");

        var detail = await detailResponse.Content
            .ReadFromJsonAsync<RecipeDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal(request.Title, detail.Title);
        Assert.Equal(request.Slug, detail.Slug);
        Assert.Equal(request.Description, detail.Description);
        Assert.Equal(request.CategoryId, detail.CategoryId);
        Assert.Equal(request.PrepTimeMinutes, detail.PrepTimeMinutes);
        Assert.Equal(request.CookTimeMinutes, detail.CookTimeMinutes);
        Assert.Equal(request.Servings, detail.Servings);
        Assert.Equal(request.Difficulty, detail.Difficulty);
    }

    private sealed record CreateRecipeResponse(Guid Id);

    private sealed record RecipeDetailResponse(
        Guid Id,
        string Title,
        string Slug,
        string Description,
        Guid CategoryId,
        string AuthorId,
        int PrepTimeMinutes,
        int CookTimeMinutes,
        int Servings,
        int Difficulty);

    private sealed record ApiInformation(
        string Name,
        string Version);
}
