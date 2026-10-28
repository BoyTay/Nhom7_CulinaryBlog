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

    [Fact]
    public async Task ListAndDetailIncludeCategoryAuthorAndPrimaryThumbnail()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var references = await RecipeIntegrationTestData.CreateReferencesAsync(
            _factory.Services,
            RecipeIntegrationTestData.CreateAuthorId());
        var withImage = Recipe.Create(
            "Recipe with thumbnail",
            $"with-thumbnail-{Guid.NewGuid():N}",
            "Has a primary image with variants.",
            references.CategoryId,
            references.AuthorId,
            5,
            20,
            2,
            RecipeDifficulty.Easy);
        var image = withImage.AddImage("https://images.example.test/original.png", "Original");
        withImage.UpdateImageVariants(
            image.Id,
            "https://images.example.test/medium.webp",
            "https://images.example.test/thumb.webp");
        var withoutImage = Recipe.Create(
            "Recipe without image",
            $"without-image-{Guid.NewGuid():N}",
            "Has no image.",
            references.CategoryId,
            references.AuthorId,
            5,
            20,
            2,
            RecipeDifficulty.Easy);
        dbContext.Recipes.AddRange(withImage, withoutImage);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var category = await dbContext.Categories.SingleAsync(c => c.Id == references.CategoryId);
        var author = await dbContext.Users.SingleAsync(u => u.Id == references.AuthorId);

        var repository = scope.ServiceProvider.GetRequiredService<IRecipeReadRepository>();
        var list = await repository.ListAsync(authorId: references.AuthorId);
        var detail = await repository.GetDetailBySlugAsync(withImage.Slug, references.AuthorId);

        var listed = list.Single(item => item.Id == withImage.Id);
        Assert.Equal(category.Name, listed.CategoryName);
        Assert.Equal(category.Slug, listed.CategorySlug);
        Assert.Equal(author.DisplayName, listed.AuthorName);
        Assert.Equal("https://images.example.test/thumb.webp", listed.ThumbnailUrl);
        Assert.Null(list.Single(item => item.Id == withoutImage.Id).ThumbnailUrl);
        Assert.NotNull(detail);
        Assert.Equal(category.Name, detail.CategoryName);
        Assert.Equal(category.Slug, detail.CategorySlug);
        Assert.Equal(author.DisplayName, detail.AuthorName);
    }
}
