using CulinaryBlog.Domain.Exceptions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void CreateStartsAsDraft()
    {
        var recipe = CreateRecipe();

        Assert.Equal(RecipeStatus.Draft, recipe.Status);
        Assert.Empty(recipe.Steps);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void PublishRequiresStepsAndIngredients()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainException>(() => recipe.Publish());

        Assert.Equal("RECIPE_NOT_READY", exception.Code);
    }

    [Fact]
    public void RemovingStepRenumbersRemainingSteps()
    {
        var recipe = CreateRecipe();
        var firstStep = recipe.AddStep("Prepare the ingredients.");
        recipe.AddStep("Cook until tender.");
        recipe.AddStep("Serve immediately.");

        recipe.RemoveStep(firstStep.Id);

        Assert.Equal([1, 2], recipe.Steps.OrderBy(step => step.StepNumber).Select(step => step.StepNumber));
    }

    [Fact]
    public void AddingAndRemovingPrimaryImagePreservesSinglePrimary()
    {
        var recipe = CreateRecipe();
        var firstImage = recipe.AddImage("https://cdn.example.com/first.jpg");
        var secondImage = recipe.AddImage("https://cdn.example.com/second.jpg");

        recipe.SetPrimaryImage(secondImage.Id);
        recipe.RemoveImage(secondImage.Id);

        Assert.True(firstImage.IsPrimary);
        Assert.Single(recipe.Images, image => image.IsPrimary);
    }

    [Fact]
    public void PublishSucceedsWithAtLeastOneStepAndIngredient()
    {
        var recipe = CreateRecipe();
        recipe.AddStep("Cook the dish.");
        recipe.AddIngredient("Rice", 100, "g");

        recipe.Publish();

        Assert.Equal(RecipeStatus.Published, recipe.Status);
    }

    private static Recipe CreateRecipe() => Recipe.Create(
        "Simple rice",
        "simple-rice",
        "A simple rice recipe.",
        CategoryId,
        "author-1",
        5,
        20,
        2,
        RecipeDifficulty.Easy);
}