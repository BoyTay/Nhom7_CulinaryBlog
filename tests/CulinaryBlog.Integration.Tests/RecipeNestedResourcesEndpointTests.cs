using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeNestedResourcesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _authorId = RecipeIntegrationTestData.CreateAuthorId();

    public RecipeNestedResourcesEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-User", _authorId);
    }

    [Fact]
    public async Task OpenApiDocumentsNotFoundForAllNestedResourceEndpoints()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var operations = new (string Path, string Method)[]
        {
            ("/api/v1/recipes/{recipeId}/steps", "post"),
            ("/api/v1/recipes/{recipeId}/steps/{stepId}", "put"),
            ("/api/v1/recipes/{recipeId}/steps/{stepId}", "delete"),
            ("/api/v1/recipes/{recipeId}/ingredients", "post"),
            ("/api/v1/recipes/{recipeId}/ingredients/{ingredientId}", "put"),
            ("/api/v1/recipes/{recipeId}/ingredients/{ingredientId}", "delete"),
        };

        foreach (var (path, method) in operations)
        {
            var responses = paths.GetProperty(path).GetProperty(method).GetProperty("responses");
            Assert.True(responses.TryGetProperty("404", out _), $"Expected 404 metadata for {method.ToUpperInvariant()} {path}.");
        }
    }

    [Fact]
    public async Task NestedMutationsReturnNotFoundForMissingRecipesAndChildrenOutsideRecipe()
    {
        using var missingRecipeRequest = CreateMutation(
            HttpMethod.Post,
            $"/api/v1/recipes/{Guid.NewGuid()}/steps",
            new RecipeStepRequest("Step", null, null),
            "\"1\"");
        using var missingRecipeResponse = await _client.SendAsync(missingRecipeRequest);
        Assert.Equal(HttpStatusCode.NotFound, missingRecipeResponse.StatusCode);

        var firstRecipeId = await CreateRecipeAsync();
        var firstRecipe = await GetRecipeAsync(firstRecipeId);
        var missingStepId = Guid.NewGuid();
        using var missingStepRequest = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{firstRecipeId}/steps/{missingStepId}",
            new RecipeStepRequest("Missing step", null, null),
            GetEtag(firstRecipe.Response));
        using var missingStepResponse = await _client.SendAsync(missingStepRequest);
        Assert.Equal(HttpStatusCode.NotFound, missingStepResponse.StatusCode);

        using var missingIngredientRequest = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{firstRecipeId}/ingredients/{Guid.NewGuid()}",
            content: null,
            GetEtag(firstRecipe.Response));
        using var missingIngredientResponse = await _client.SendAsync(missingIngredientRequest);
        Assert.Equal(HttpStatusCode.NotFound, missingIngredientResponse.StatusCode);

        var secondRecipeId = await CreateRecipeAsync();
        var secondRecipe = await GetRecipeAsync(secondRecipeId);
        var stepFromSecondRecipe = await CreateStepAsync(
            secondRecipeId,
            "Belongs to second recipe",
            secondRecipe.Response);
        var ingredientFromSecondRecipe = await CreateIngredientAsync(
            secondRecipeId,
            "Ingredient from second recipe",
            1,
            "piece",
            stepFromSecondRecipe);

        using var foreignStepRequest = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{firstRecipeId}/steps/{await GetIdAsync(stepFromSecondRecipe)}",
            content: null,
            GetEtag(firstRecipe.Response));
        using var foreignStepResponse = await _client.SendAsync(foreignStepRequest);
        using var foreignIngredientRequest = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{firstRecipeId}/ingredients/{await GetIdAsync(ingredientFromSecondRecipe)}",
            content: null,
            GetEtag(firstRecipe.Response));
        using var foreignIngredientResponse = await _client.SendAsync(foreignIngredientRequest);

        Assert.Equal(HttpStatusCode.NotFound, foreignStepResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignIngredientResponse.StatusCode);
    }

    [Fact]
    public async Task IngredientMutationsEnforceOwnerAndAdminAuthorization()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);
        var etag = GetEtag(recipe.Response);

        using var nonOwnerCreate = CreateMutation(
            HttpMethod.Post,
            $"/api/v1/recipes/{recipeId}/ingredients",
            new RecipeIngredientRequest("Flour", 100, "g", null),
            etag);
        nonOwnerCreate.Headers.Add("X-Test-User", "another-author");
        using var deniedCreate = await _client.SendAsync(nonOwnerCreate);
        Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);

        var ownerCreated = await CreateIngredientAsync(recipeId, "Rice", 200, "g", recipe.Response);
        var ingredientId = await GetIdAsync(ownerCreated);
        var currentEtag = GetEtag(ownerCreated);
        var ingredientPath = $"/api/v1/recipes/{recipeId}/ingredients/{ingredientId}";

        using var nonOwnerUpdate = CreateMutation(
            HttpMethod.Put,
            ingredientPath,
            new RecipeIngredientRequest("Forbidden update", 300, "g", null),
            currentEtag);
        nonOwnerUpdate.Headers.Add("X-Test-User", "another-author");
        using var deniedUpdate = await _client.SendAsync(nonOwnerUpdate);

        using var nonOwnerDelete = CreateMutation(
            HttpMethod.Delete,
            ingredientPath,
            content: null,
            currentEtag);
        nonOwnerDelete.Headers.Add("X-Test-User", "another-author");
        using var deniedDelete = await _client.SendAsync(nonOwnerDelete);

        Assert.Equal(HttpStatusCode.Forbidden, deniedUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);

        using var adminCreate = CreateMutation(
            HttpMethod.Post,
            $"/api/v1/recipes/{recipeId}/ingredients",
            new RecipeIngredientRequest("Salt", 1, "g", null),
            currentEtag);
        adminCreate.Headers.Add("X-Test-User", "recipe-admin");
        adminCreate.Headers.Add("X-Test-Role", "Admin");
        using var createdByAdmin = await _client.SendAsync(adminCreate);
        Assert.Equal(HttpStatusCode.Created, createdByAdmin.StatusCode);
        var adminIngredientId = await GetIdAsync(createdByAdmin);

        using var adminUpdate = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{recipeId}/ingredients/{adminIngredientId}",
            new RecipeIngredientRequest("Sea salt", 2, "g", null),
            GetEtag(createdByAdmin));
        adminUpdate.Headers.Add("X-Test-User", "recipe-admin");
        adminUpdate.Headers.Add("X-Test-Role", "Admin");
        using var updatedByAdmin = await _client.SendAsync(adminUpdate);
        Assert.Equal(HttpStatusCode.OK, updatedByAdmin.StatusCode);

        using var adminDelete = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/ingredients/{adminIngredientId}",
            content: null,
            GetEtag(updatedByAdmin));
        adminDelete.Headers.Add("X-Test-User", "recipe-admin");
        adminDelete.Headers.Add("X-Test-Role", "Admin");
        using var deletedByAdmin = await _client.SendAsync(adminDelete);
        Assert.Equal(HttpStatusCode.NoContent, deletedByAdmin.StatusCode);
    }

    [Fact]
    public async Task StepCrudMaintainsContinuousNumbersAndUpdatesRecipeEtag()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);

        var first = await CreateStepAsync(recipeId, "Prepare ingredients", recipe.Response);
        var second = await CreateStepAsync(recipeId, "Cook ingredients", first);
        var third = await CreateStepAsync(recipeId, "Serve", second);
        var secondId = await GetIdAsync(second);
        var thirdId = await GetIdAsync(third);

        using var update = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{recipeId}/steps/{thirdId}",
            new RecipeStepRequest("Plate and serve", 2, null),
            GetEtag(third));
        using var updated = await _client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.NotEqual(GetEtag(third), GetEtag(updated));

        using var delete = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/steps/{secondId}",
            content: null,
            GetEtag(updated));
        using var deleted = await _client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var final = await GetRecipeAsync(recipeId);
        Assert.Equal([1, 2], final.Detail.Steps.OrderBy(step => step.StepNumber).Select(step => step.StepNumber));
        Assert.Equal("Plate and serve", final.Detail.Steps.Single(step => step.Id == thirdId).Description);
    }

    [Fact]
    public async Task IngredientCrudMaintainsContinuousSortOrder()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);

        var first = await CreateIngredientAsync(recipeId, "Rice", 200, "g", recipe.Response);
        var second = await CreateIngredientAsync(recipeId, "Water", 300, "ml", first);
        var third = await CreateIngredientAsync(recipeId, "Salt", 2, "g", second);
        var firstId = await GetIdAsync(first);
        var secondId = await GetIdAsync(second);

        using var update = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{recipeId}/ingredients/{secondId}",
            new RecipeIngredientRequest("Filtered water", 350, "ml", "Warm"),
            GetEtag(third));
        using var updated = await _client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var delete = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/ingredients/{firstId}",
            content: null,
            GetEtag(updated));
        using var deleted = await _client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var final = await GetRecipeAsync(recipeId);
        var ingredients = final.Detail.Ingredients.OrderBy(ingredient => ingredient.SortOrder).ToArray();
        Assert.Equal([1, 2], ingredients.Select(ingredient => ingredient.SortOrder));
        Assert.Equal("Filtered water", ingredients[0].Name);
        Assert.Equal("Salt", ingredients[1].Name);
    }

    [Fact]
    public async Task NestedMutationsRequireCurrentEtagAndOwnerOrAdminAuthorization()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);
        var step = await CreateStepAsync(recipeId, "Owner step", recipe.Response);
        var stepId = await GetIdAsync(step);
        var currentEtag = GetEtag(step);

        using var missingEtag = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/recipes/{recipeId}/ingredients")
        {
            Content = JsonContent.Create(new RecipeIngredientRequest("Flour", 100, "g", null)),
        };
        using var missingEtagResponse = await _client.SendAsync(missingEtag);

        using var nonOwnerRequest = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{recipeId}/steps/{stepId}",
            new RecipeStepRequest("Forbidden update", null, null),
            currentEtag);
        nonOwnerRequest.Headers.Add("X-Test-User", "another-author");
        using var nonOwnerResponse = await _client.SendAsync(nonOwnerRequest);

        using var adminRequest = CreateMutation(
            HttpMethod.Put,
            $"/api/v1/recipes/{recipeId}/steps/{stepId}",
            new RecipeStepRequest("Admin update", null, null),
            currentEtag);
        adminRequest.Headers.Add("X-Test-User", "recipe-admin");
        adminRequest.Headers.Add("X-Test-Role", "Admin");
        using var adminResponse = await _client.SendAsync(adminRequest);

        Assert.Equal((HttpStatusCode)428, missingEtagResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, nonOwnerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var staleRequest = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/steps/{stepId}",
            content: null,
            currentEtag);
        using var staleResponse = await _client.SendAsync(staleRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, staleResponse.StatusCode);
        using var problem = JsonDocument.Parse(await staleResponse.Content.ReadAsStringAsync());
        Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", problem.RootElement.GetProperty("type").GetString());
    }

    private async Task<Guid> CreateRecipeAsync()
    {
        var references = await RecipeIntegrationTestData.CreateReferencesAsync(
            _factory.Services,
            _authorId);
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeRequest(
                "Nested resource recipe",
                $"nested-resource-{Guid.NewGuid():N}",
                "Recipe for nested-resource integration tests.",
                references.CategoryId,
                5,
                15,
                2,
                RecipeDifficulty.Easy));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> CreateStepAsync(Guid recipeId, string description, HttpResponseMessage previous)
    {
        using var request = CreateMutation(
            HttpMethod.Post,
            $"/api/v1/recipes/{recipeId}/steps",
            new RecipeStepRequest(description, null, null),
            GetEtag(previous));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response;
    }

    private async Task<HttpResponseMessage> CreateIngredientAsync(
        Guid recipeId,
        string name,
        decimal quantity,
        string unit,
        HttpResponseMessage previous)
    {
        using var request = CreateMutation(
            HttpMethod.Post,
            $"/api/v1/recipes/{recipeId}/ingredients",
            new RecipeIngredientRequest(name, quantity, unit, null),
            GetEtag(previous));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response;
    }

    private async Task<(HttpResponseMessage Response, RecipeDetailDto Detail)> GetRecipeAsync(Guid recipeId)
    {
        var response = await _client.GetAsync($"/api/v1/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<RecipeDetailDto>(TestJson.Options);
        Assert.NotNull(detail);
        return (response, detail);
    }

    private static HttpRequestMessage CreateMutation(HttpMethod method, string uri, object? content, string etag)
    {
        var request = new HttpRequestMessage(method, uri);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static async Task<Guid> GetIdAsync(HttpResponseMessage response)
    {
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static string GetEtag(HttpResponseMessage response) => response.Headers.GetValues("ETag").Single();
}
