using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeReadRepositoryTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RecipeReadRepositoryTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ProjectedListAndSplitDetailDoNotTrackRecipeGraph()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var references = await RecipeIntegrationTestData.CreateReferencesAsync(
            _factory.Services,
            RecipeIntegrationTestData.CreateAuthorId());
        var recipe = Recipe.Create(
            "Projected read recipe",
            $"projected-read-{Guid.NewGuid():N}",
            "Verifies the optimized Recipe read path.",
            references.CategoryId,
            references.AuthorId,
            5,
            20,
            2,
            RecipeDifficulty.Medium);
        recipe.AddStep("Prepare the projected recipe.");
        recipe.AddIngredient("Projection ingredient", 1, "piece");
        recipe.AddImage("https://images.example.test/culinary-blog/projected.png", "Projected image");
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var repository = scope.ServiceProvider.GetRequiredService<IRecipeReadRepository>();
        var list = await repository.ListAsync(authorId: recipe.AuthorId);
        var detail = await repository.GetDetailAsync(recipe.Id, recipe.AuthorId);

        Assert.Contains(list, item => item.Id == recipe.Id);
        Assert.NotNull(detail);
        Assert.Single(detail.Steps);
        Assert.Single(detail.Ingredients);
        Assert.Single(detail.Images);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }
}
