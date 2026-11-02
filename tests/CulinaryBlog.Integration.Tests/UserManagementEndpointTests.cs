using System.Net;
using System.Net.Http.Json;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Users;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Domain.Users;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests;

public sealed class UserManagementEndpointTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private static readonly string Password = $"Aa1!{Guid.NewGuid():N}";

    private sealed record UserList(ManagedUserSummaryDto Summary, UserPage Users);

    private sealed record UserPage(IReadOnlyList<ManagedUserDto> Items, int TotalCount, int Page, int PageSize, int TotalPages);

    [Fact]
    public async Task EveryEndpointIsAdminOnly()
    {
        var client = Client("author-only", Roles.Author);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/v1/users/x/role", new { role = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/v1/users/x/lock", new { locked = true })).StatusCode);
    }

    [Fact]
    public async Task AccountsCannotBeDeletedThroughTheApi()
    {
        var admin = await CreateAccountAsync("No Delete Admin", Roles.Admin);
        var target = await CreateAccountAsync("No Delete Target", Roles.Author);

        var response = await Client(admin.Id, Roles.Admin).DeleteAsync($"/api/v1/users/{target.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await FindAccountAsync(target.Id));
    }

    [Fact]
    public async Task ListSearchesWithoutAccentsFiltersPagesAndCountsEveryAccount()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var admin = await CreateAccountAsync($"Nguyễn Hoài {token} Admin", Roles.Admin);
        var author = await CreateAccountAsync($"Nguyễn Hoài {token} Tác giả", Roles.Author);
        var locked = await CreateAccountAsync($"Nguyễn Hoài {token} Khóa", Roles.Author, active: false);
        await AddRecipeAsync(author.Id);
        var client = Client(admin.Id, Roles.Admin);

        var all = await ListAsync(client, $"q={Uri.EscapeDataString($"nguyen hoai {token}")}");
        Assert.Equal([admin.Id, author.Id, locked.Id], all.Users.Items.Select(user => user.Id));
        Assert.Equal(1, all.Users.Items.Single(user => user.Id == author.Id).RecipeCount);
        Assert.Equal(Roles.Admin, all.Users.Items.Single(user => user.Id == admin.Id).Role);
        Assert.Equal(ManagedUserStatus.Locked, all.Users.Items.Single(user => user.Id == locked.Id).Status);

        var admins = await ListAsync(client, $"q={token}&role=Admin");
        Assert.Equal(admin.Id, Assert.Single(admins.Users.Items).Id);

        var authors = await ListAsync(client, $"q={token}&role=Author&status=Active");
        Assert.Equal(author.Id, Assert.Single(authors.Users.Items).Id);

        var lockedOnly = await ListAsync(client, $"q={token}&status=Locked");
        Assert.Equal(locked.Id, Assert.Single(lockedOnly.Users.Items).Id);

        var secondPage = await ListAsync(client, $"q={token}&page=2&pageSize=2");
        Assert.Equal(3, secondPage.Users.TotalCount);
        Assert.Equal(2, secondPage.Users.TotalPages);
        Assert.Equal(locked.Id, Assert.Single(secondPage.Users.Items).Id);

        // The summary ignores the filter, and it adds up.
        Assert.True(admins.Summary.Total >= 3);
        Assert.Equal(admins.Summary.Total, admins.Summary.Admins + admins.Summary.Authors);
        Assert.True(admins.Summary.Locked >= 1);
        Assert.True(admins.Summary.JoinedLast30Days >= 3);
    }

    [Fact]
    public async Task SearchTreatsLikeWildcardsAsPlainText()
    {
        var admin = await CreateAccountAsync("Wildcard Admin", Roles.Admin);

        var result = await ListAsync(Client(admin.Id, Roles.Admin), $"q={Uri.EscapeDataString("%_%_%_%_%_%_%_%")}");

        Assert.Empty(result.Users.Items);
    }

    [Fact]
    public async Task InvalidFilterAndRoleAreRejected()
    {
        var admin = await CreateAccountAsync("Validation Admin", Roles.Admin);
        var author = await CreateAccountAsync("Validation Author", Roles.Author);
        var client = Client(admin.Id, Roles.Admin);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/api/v1/users?role=Owner")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/api/v1/users?pageSize=51")).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await client.PutAsJsonAsync($"/api/v1/users/{author.Id}/role", new { role = "Owner" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await client.PutAsJsonAsync($"/api/v1/users/{author.Id}/lock", new { locked = true, days = 0 })).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await client.PutAsJsonAsync($"/api/v1/users/{author.Id}/lock", new { locked = false, days = 7 })).StatusCode);
    }

    [Fact]
    public async Task AnAdminCannotChangeTheirOwnAccount()
    {
        var admin = await CreateAccountAsync("Self Admin", Roles.Admin);
        var client = Client(admin.Id, Roles.Admin);

        await AssertProblemAsync(
            await client.PutAsJsonAsync($"/api/v1/users/{admin.Id}/role", new { role = "Author" }),
            HttpStatusCode.Forbidden,
            "USER_SELF_MANAGEMENT_FORBIDDEN");
        await AssertProblemAsync(
            await client.PutAsJsonAsync($"/api/v1/users/{admin.Id}/lock", new { locked = true }),
            HttpStatusCode.Forbidden,
            "USER_SELF_MANAGEMENT_FORBIDDEN");
    }

    [Fact]
    public async Task ATokenOlderThanTheDemotionNoLongerManagesAccounts()
    {
        var demoted = await CreateAccountAsync("Demoted Admin", Roles.Author);
        var target = await CreateAccountAsync("Bystander", Roles.Author);

        await AssertProblemAsync(
            await Client(demoted.Id, Roles.Admin).PutAsJsonAsync($"/api/v1/users/{target.Id}/lock", new { locked = true }),
            HttpStatusCode.Forbidden,
            "ADMIN_RIGHTS_REVOKED");
        Assert.True((await FindAccountAsync(target.Id))!.IsActive);
    }

    [Fact]
    public async Task UnknownAccountReturnsNotFound()
    {
        var admin = await CreateAccountAsync("Lookup Admin", Roles.Admin);

        await AssertProblemAsync(
            await Client(admin.Id, Roles.Admin).PutAsJsonAsync("/api/v1/users/does-not-exist/lock", new { locked = true }),
            HttpStatusCode.NotFound,
            "USER_NOT_FOUND");
    }

    [Fact]
    public async Task RoleChangeReachesTheUsersNextLookupAndKeepsAuthorRights()
    {
        var admin = await CreateAccountAsync("Role Admin", Roles.Admin);
        var target = await CreateAccountAsync("Role Target", Roles.Author);
        var client = Client(admin.Id, Roles.Admin);

        var promoted = await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/role", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.NoContent, promoted.StatusCode);
        Assert.Equal([Roles.Admin, Roles.Author], (await RolesOfAsync(target.Id)).Order());

        var demoted = await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/role", new { role = "Author" });
        Assert.Equal(HttpStatusCode.NoContent, demoted.StatusCode);
        Assert.Equal([Roles.Author], await RolesOfAsync(target.Id));
    }

    [Fact]
    public async Task LockBlocksSignInRevokesSessionsAndSurvivesAPasswordReset()
    {
        var admin = await CreateAccountAsync("Lock Admin", Roles.Admin);
        var target = await CreateAccountAsync("Lock Target", Roles.Author);
        await AddRefreshTokenAsync(target.Id);
        var client = Client(admin.Id, Roles.Admin);

        var lockResponse = await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/lock", new { locked = true });
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            Assert.True(await identity.IsLockedOutAsync(target.Email!));
            Assert.Null(await identity.ValidateCredentialsAsync(target.Email!, Password));

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.DoesNotContain(
                await dbContext.RefreshTokens.Where(token => token.UserId == target.Id).ToListAsync(),
                token => !token.IsRevoked);

            // Reset proves mailbox control, which lifts a brute-force lockout but not an admin's decision.
            var ticket = await identity.CreatePasswordResetTicketAsync(target.Email!);
            await identity.ResetPasswordAsync(target.Email!, ticket!.Token, Password);
            Assert.True(await identity.IsLockedOutAsync(target.Email!));
        }

        var unlockResponse = await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/lock", new { locked = false });
        Assert.Equal(HttpStatusCode.NoContent, unlockResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            Assert.False(await identity.IsLockedOutAsync(target.Email!));
            Assert.NotNull(await identity.ValidateCredentialsAsync(target.Email!, Password));
        }
    }

    [Fact]
    public async Task ATimedLockReportsItsEndAndAPermanentOneHasNone()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var admin = await CreateAccountAsync("Timed Admin", Roles.Admin);
        var timed = await CreateAccountAsync($"Timed {token}", Roles.Author);
        var permanent = await CreateAccountAsync($"Permanent {token}", Roles.Author);
        var client = Client(admin.Id, Roles.Admin);
        var before = DateTimeOffset.UtcNow;

        await client.PutAsJsonAsync($"/api/v1/users/{timed.Id}/lock", new { locked = true, days = 7 });
        await client.PutAsJsonAsync($"/api/v1/users/{permanent.Id}/lock", new { locked = true });

        var list = await ListAsync(client, $"q={token}&status=Locked");
        var timedRow = list.Users.Items.Single(user => user.Id == timed.Id);
        var permanentRow = list.Users.Items.Single(user => user.Id == permanent.Id);
        Assert.Equal(7, timedRow.LockDays);
        Assert.InRange(timedRow.LockedUntil!.Value, before.AddDays(7), DateTimeOffset.UtcNow.AddDays(7));
        Assert.Null(permanentRow.LockDays);
        Assert.Null(permanentRow.LockedUntil);
        Assert.Equal(ManagedUserStatus.Locked, permanentRow.Status);
    }

    [Fact]
    public async Task ALockPastItsEndNoLongerBlocksOrCountsAndUnlockClearsIt()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var admin = await CreateAccountAsync("Expiry Admin", Roles.Admin);
        var target = await CreateAccountAsync($"Expiry {token}", Roles.Author);
        var client = Client(admin.Id, Roles.Admin);
        await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/lock", new { locked = true, days = 1 });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.SingleAsync(item => item.Id == target.Id);
            user.LockedAt = DateTimeOffset.UtcNow.AddDays(-2);
            user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            Assert.False(await identity.IsLockedOutAsync(target.Email!));
            Assert.NotNull(await identity.ValidateCredentialsAsync(target.Email!, Password));
        }

        Assert.Empty((await ListAsync(client, $"q={token}&status=Locked")).Users.Items);
        var row = Assert.Single((await ListAsync(client, $"q={token}&status=Active")).Users.Items);
        Assert.Equal(ManagedUserStatus.Active, row.Status);
        Assert.Null(row.LockedUntil);
        Assert.Null(row.LockDays);

        await client.PutAsJsonAsync($"/api/v1/users/{target.Id}/lock", new { locked = false });
        var unlocked = await FindAccountAsync(target.Id);
        Assert.True(unlocked!.IsActive);
        Assert.Null(unlocked.LockedAt);
        Assert.Null(unlocked.LockedUntil);
    }

    private HttpClient Client(string userId, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task<UserList> ListAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/v1/users?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserList>(TestJson.Options))!;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string type)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        Assert.Equal(type, problem!.Type);
    }

    private async Task<ApplicationUser> CreateAccountAsync(string displayName, string role, bool active = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var id = RecipeIntegrationTestData.CreateAuthorId();
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test",
            DisplayName = displayName,
            IsActive = active,
        };

        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        foreach (var name in role == Roles.Admin ? new[] { Roles.Admin, Roles.Author } : [Roles.Author])
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new IdentityRole(name));
            }

            await userManager.AddToRoleAsync(user, name);
        }

        return user;
    }

    private async Task<ApplicationUser?> FindAccountAsync(string userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId);
    }

    private async Task<IList<string>> RolesOfAsync(string userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        return (await identity.FindByIdAsync(userId))!.Roles.ToList();
    }

    private async Task<Guid> AddRecipeAsync(string authorId)
    {
        var references = await RecipeIntegrationTestData.CreateReferencesAsync(factory.Services, authorId);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recipe = Recipe.Create(
            "User management recipe",
            $"user-management-{Guid.NewGuid():N}",
            "Recipe owned by a managed account.",
            references.CategoryId,
            authorId,
            5,
            10,
            2,
            RecipeDifficulty.Easy);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();
        return recipe.Id;
    }

    private async Task AddRefreshTokenAsync(string userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.RefreshTokens.Add(RefreshToken.Create(
            userId,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            "127.0.0.1"));
        await dbContext.SaveChangesAsync();
    }
}
