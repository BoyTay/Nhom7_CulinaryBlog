using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests.Auth;

/// <summary>
/// W08: the Recipe module consumes the Author/Admin claims of real access tokens issued by Auth
/// (no test authentication handler), with the AuthorId foreign key to AspNetUsers enforced.
/// </summary>
public sealed class AuthRecipeClaimsTests(AuthDatabaseFactory factory) : IClassFixture<AuthDatabaseFactory>
{
    private static int s_clientId = 500;

    [Fact]
    public async Task RecipeOwnershipFollowsJwtSubjectAndAdminRoleClaim()
    {
        var categoryId = await CreateCategoryAsync();
        var owner = await RegisterAsync();
        var otherAuthor = await RegisterAsync();
        using var api = CreateClient();

        using var anonymous = await api.PostAsJsonAsync("/api/v1/recipes", NewRecipe(categoryId));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var recipeId = await CreateRecipeAsync(api, owner.AccessToken, categoryId);
        var (detail, etag) = await GetDetailAsync(api, owner.AccessToken, recipeId);
        // The JWT "sub" claim becomes Recipe.AuthorId and satisfies the AspNetUsers foreign key.
        Assert.Equal(owner.User.Id, detail.GetProperty("authorId").GetString());

        using var hiddenDraft = await SendAsync(api, HttpMethod.Get, $"/api/v1/recipes/{recipeId}", otherAuthor.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, hiddenDraft.StatusCode);
        using var forbidden = await UpdateAsync(api, otherAuthor.AccessToken, recipeId, categoryId, etag, "Not my recipe");
        await AssertProblemTypeAsync(forbidden, HttpStatusCode.Forbidden, "RECIPE_FORBIDDEN");

        var admin = await LoginAsync(factory.AdminEmail, factory.AdminPassword);
        Assert.Contains(Roles.Admin, admin.User.Roles);
        using var adminUpdate = await UpdateAsync(api, admin.AccessToken, recipeId, categoryId, etag, "Edited by admin");
        Assert.Equal(HttpStatusCode.OK, adminUpdate.StatusCode);

        var newEtag = adminUpdate.Headers.ETag?.Tag ?? throw new InvalidOperationException("ETag missing");
        using var ownerUpdate = await UpdateAsync(api, owner.AccessToken, recipeId, categoryId, newEtag, "Edited by owner");
        Assert.Equal(HttpStatusCode.OK, ownerUpdate.StatusCode);
    }

    [Fact]
    public async Task RoleChangesReachRecipeOnlyAfterRefreshReissuesClaims()
    {
        var categoryId = await CreateCategoryAsync();
        var owner = await RegisterAsync();
        var promoted = await RegisterAsync();
        using var api = CreateClient();
        var recipeId = await CreateRecipeAsync(api, owner.AccessToken, categoryId);
        var (_, etag) = await GetDetailAsync(api, owner.AccessToken, recipeId);

        await ChangeRolesAsync(promoted.User.Id, add: Roles.Admin);
        await ChangeRolesAsync(owner.User.Id, remove: Roles.Author);

        // Access tokens are a 15-minute snapshot of roles: the old token keeps its old claims.
        using var staleAdmin = await UpdateAsync(api, promoted.AccessToken, recipeId, categoryId, etag, "Stale admin claim");
        Assert.Equal(HttpStatusCode.Forbidden, staleAdmin.StatusCode);

        // Refresh re-reads roles from the database.
        var refreshedAdmin = await RefreshAsync(promoted.RefreshToken);
        Assert.Contains(Roles.Admin, refreshedAdmin.User.Roles);
        using var adminUpdate = await UpdateAsync(api, refreshedAdmin.AccessToken, recipeId, categoryId, etag, "Fresh admin claim");
        Assert.Equal(HttpStatusCode.OK, adminUpdate.StatusCode);

        var demoted = await RefreshAsync(owner.RefreshToken);
        Assert.DoesNotContain(Roles.Author, demoted.User.Roles);
        using var demotedCreate = await SendAsync(api, HttpMethod.Post, "/api/v1/recipes", demoted.AccessToken, NewRecipe(categoryId));
        Assert.Equal(HttpStatusCode.Forbidden, demotedCreate.StatusCode);
    }

    private async Task<Guid> CreateCategoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = Category.Create($"Claims category {suffix}", $"claims-category-{suffix}");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task ChangeRolesAsync(string userId, string? add = null, string? remove = null)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("User missing");
        if (add is not null) Assert.True((await users.AddToRoleAsync(user, add)).Succeeded);
        if (remove is not null) Assert.True((await users.RemoveFromRoleAsync(user, remove)).Succeeded);
    }

    private static object NewRecipe(Guid categoryId)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new
        {
            title = $"Claims recipe {suffix}",
            slug = $"claims-recipe-{suffix}",
            description = "Recipe used to verify Auth claims.",
            categoryId,
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            servings = 2,
            difficulty = 0,
        };
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient api, string accessToken, Guid categoryId)
    {
        using var created = await SendAsync(api, HttpMethod.Post, "/api/v1/recipes", accessToken, NewRecipe(categoryId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(JsonElement Detail, string ETag)> GetDetailAsync(HttpClient api, string accessToken, Guid recipeId)
    {
        using var response = await SendAsync(api, HttpMethod.Get, $"/api/v1/recipes/{recipeId}", accessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var etag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException("ETag missing");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.Clone(), etag);
    }

    private static Task<HttpResponseMessage> UpdateAsync(
        HttpClient api, string accessToken, Guid recipeId, Guid categoryId, string etag, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{recipeId}")
        {
            Content = JsonContent.Create(new
            {
                title,
                slug = $"claims-update-{Guid.NewGuid():N}",
                description = "Updated through a real access token.",
                categoryId,
                prepTimeMinutes = 10,
                cookTimeMinutes = 20,
                servings = 2,
                difficulty = 0,
                calories = (int?)null,
                proteinGrams = (decimal?)null,
                carbohydratesGrams = (decimal?)null,
                fatGrams = (decimal?)null,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return api.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient api, HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return api.SendAsync(request);
    }

    private async Task<Session> RegisterAsync()
    {
        // Each user gets its own client address so the per-IP auth rate limit never interferes.
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Claims Chef",
            email = $"claims-{Guid.NewGuid():N}@example.test",
            password = $"Aa1!{Guid.NewGuid():N}",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ToSessionAsync(response);
    }

    private async Task<Session> LoginAsync(string email, string password)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ToSessionAsync(response);
    }

    private async Task<Session> RefreshAsync(string refreshToken)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"rt={refreshToken}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ToSessionAsync(response);
    }

    private static async Task<Session> ToSessionAsync(HttpResponseMessage response)
    {
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var refresh = WebUtility.UrlDecode(cookie.Split(';')[0]["rt=".Length..]);
        return new Session(auth.AccessToken, refresh, auth.User);
    }

    private static async Task AssertProblemTypeAsync(HttpResponseMessage response, HttpStatusCode status, string type)
    {
        Assert.Equal(status, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(type, problem.RootElement.GetProperty("type").GetString());
    }

    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
        var id = Interlocked.Increment(ref s_clientId);
        client.DefaultRequestHeaders.Add("X-Test-Client-IP", $"198.51.{id / 250}.{id % 250 + 1}");
        return client;
    }

    private sealed record Session(string AccessToken, string RefreshToken, AuthUserDto User);
}
