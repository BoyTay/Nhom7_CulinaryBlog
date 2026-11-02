using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeImageVariantVersionTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task VariantSaveKeepsTheRecipeVersionWhileUserImageChangesAdvanceIt()
    {
        var authorId = RecipeIntegrationTestData.CreateAuthorId();
        var references = await RecipeIntegrationTestData.CreateReferencesAsync(factory.Services, authorId);
        var recipeId = await CreateRecipeWithImageAsync(references, authorId);

        var afterUpload = await ReadVersionAsync(recipeId);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(recipeId);
            var image = Assert.Single(recipe!.Images);
            recipe.UpdateImageVariants(image.Id, "https://storage.example/m.webp", "https://storage.example/t.webp");
            await repository.SaveImageVariantChangesAsync(recipe);
        }

        Assert.Equal(afterUpload, await ReadVersionAsync(recipeId));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(recipeId);
            var image = Assert.Single(recipe!.Images);
            Assert.Equal("https://storage.example/t.webp", image.ThumbnailUrl);
            repository.AddImage(recipe.AddImage("https://storage.example/second.png"));
            await repository.SaveImageChangesAsync(recipe);
        }

        Assert.NotEqual(afterUpload, await ReadVersionAsync(recipeId));
    }

    private async Task<Guid> CreateRecipeWithImageAsync(RecipeTestReferences references, string authorId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recipe = Recipe.Create(
            "Variant version recipe",
            $"variant-version-{Guid.NewGuid():N}",
            "Recipe for variant version tests.",
            references.CategoryId,
            authorId,
            5,
            10,
            2,
            RecipeDifficulty.Easy);
        recipe.AddImage("https://storage.example/original.png");
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();
        return recipe.Id;
    }

    private async Task<uint> ReadVersionAsync(Guid recipeId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
        return (await repository.GetByIdAsync(recipeId))!.Version;
    }
}
