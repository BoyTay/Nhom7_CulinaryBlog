using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests;

internal sealed record RecipeTestReferences(Guid CategoryId, string AuthorId);

internal static class RecipeIntegrationTestData
{
    public static string CreateAuthorId() => $"recipe-test-author-{Guid.NewGuid():N}";

    public static async Task<RecipeTestReferences> CreateReferencesAsync(
        IServiceProvider services,
        string authorId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await CreateReferencesAsync(dbContext, authorId);
    }

    public static async Task<RecipeTestReferences> CreateReferencesAsync(
        ApplicationDbContext dbContext,
        string authorId)
    {
        await EnsureAuthorsAsync(dbContext, authorId);

        var suffix = Guid.NewGuid().ToString("N");
        var category = Category.Create(
            $"Recipe test category {suffix}",
            $"recipe-test-category-{suffix}");
        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync();

        return new RecipeTestReferences(category.Id, authorId);
    }

    public static async Task EnsureAuthorsAsync(
        ApplicationDbContext dbContext,
        params string[] authorIds)
    {
        var requestedIds = authorIds.Distinct(StringComparer.Ordinal).ToArray();
        var trackedIds = dbContext.Users.Local
            .Where(user => requestedIds.Contains(user.Id, StringComparer.Ordinal))
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);
        var persistedIds = await dbContext.Users
            .Where(user => requestedIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToHashSetAsync();

        foreach (var authorId in requestedIds)
        {
            if (trackedIds.Contains(authorId) || persistedIds.Contains(authorId))
            {
                continue;
            }

            dbContext.Users.Add(new ApplicationUser
            {
                Id = authorId,
                UserName = authorId,
                NormalizedUserName = authorId.ToUpperInvariant(),
                DisplayName = "Recipe Integration Test Author",
                SecurityStamp = Guid.NewGuid().ToString("N"),
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
