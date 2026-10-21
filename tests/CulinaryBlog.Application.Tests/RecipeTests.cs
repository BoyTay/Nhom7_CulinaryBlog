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
        Assert.Null(recipe.PublishedAt);
        Assert.Empty(recipe.Steps);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void PublishRequiresStepsAndIngredients()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainException>(() => recipe.Publish());

        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", exception.Code);
        Assert.Null(recipe.PublishedAt);
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
    public void UpdatingStepChangesItsContentWithoutChangingItsNumber()
    {
        var recipe = CreateRecipe();
        var step = recipe.AddStep("Old description.", 5, "https://example.com/old.jpg");

        recipe.UpdateStep(step.Id, "  New description.  ", 10, null);

        Assert.Equal(1, step.StepNumber);
        Assert.Equal("New description.", step.Description);
        Assert.Equal(10, step.TimerMinutes);
        Assert.Null(step.ImageUrl);
    }

    [Fact]
    public void RemovingIngredientRenumbersRemainingIngredients()
    {
        var recipe = CreateRecipe();
        var first = recipe.AddIngredient("Rice", 100, "g");
        recipe.AddIngredient("Water", 200, "ml");
        recipe.AddIngredient("Salt", 1, "g");

        recipe.RemoveIngredient(first.Id);

        Assert.Equal([1, 2], recipe.Ingredients.OrderBy(item => item.SortOrder).Select(item => item.SortOrder));
    }

    [Fact]
    public void UpdatingIngredientChangesItsFieldsAndPreservesSortOrder()
    {
        var recipe = CreateRecipe();
        var ingredient = recipe.AddIngredient("Rice", 100, "g", "Old note");

        recipe.UpdateIngredient(ingredient.Id, "  Brown rice  ", 150, "grams", null);

        Assert.Equal(1, ingredient.SortOrder);
        Assert.Equal("Brown rice", ingredient.Name);
        Assert.Equal(150, ingredient.Quantity);
        Assert.Equal("grams", ingredient.Unit);
        Assert.Null(ingredient.Notes);
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
        Assert.NotNull(recipe.PublishedAt);
    }

    [Fact]
    public void PublishingAnAlreadyPublishedRecipeKeepsItsPublishedAt()
    {
        var recipe = CreateRecipe();
        recipe.AddStep("Cook the dish.");
        recipe.AddIngredient("Rice", 100, "g");
        recipe.Publish();
        var publishedAt = recipe.PublishedAt;

        recipe.Publish();

        Assert.Equal(publishedAt, recipe.PublishedAt);
    }

    [Fact]
    public void UnpublishReturnsToDraftAndClearsPublishedAt()
    {
        var recipe = CreateRecipe();
        recipe.AddStep("Cook the dish.");
        recipe.AddIngredient("Rice", 100, "g");

        recipe.Publish();
        Assert.NotNull(recipe.PublishedAt);

        recipe.Unpublish();

        Assert.Equal(RecipeStatus.Draft, recipe.Status);
        Assert.Null(recipe.PublishedAt);
    }

    [Fact]
    public void ArchiveIsIdempotentAndHidesDraftWithoutPublishingIt()
    {
        var recipe = CreateRecipe();

        recipe.Archive();
        recipe.Archive();

        Assert.Equal(RecipeStatus.Archived, recipe.Status);
        Assert.Null(recipe.PublishedAt);
    }

    [Fact]
    public void ArchivedRecipeCanBeRepublishedWhenRequirementsRemainValid()
    {
        var recipe = CreateRecipe();
        recipe.AddStep("Cook the dish.");
        recipe.AddIngredient("Rice", 100, "g");
        recipe.Publish();
        recipe.Archive();

        recipe.Publish();

        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
    }

    [Fact]
    public void RemovingLastImageAllowsNewPrimary()
    {
        var recipe = CreateRecipe();
        var image = recipe.AddImage("https://cdn.example.com/first.jpg");
        recipe.RemoveImage(image.Id);

        Assert.Empty(recipe.Images);
        var replacement = recipe.AddImage("https://cdn.example.com/replacement.jpg");
        Assert.True(replacement.IsPrimary);
        Assert.Equal(0, replacement.OrderIndex);
    }

    [Fact]
    public void RemovingNonPrimaryPreservesPrimaryAndCompactsOrder()
    {
        var recipe = CreateRecipe();
        var first = recipe.AddImage("https://cdn.example.com/first.jpg");
        var middle = recipe.AddImage("https://cdn.example.com/middle.jpg");
        var last = recipe.AddImage("https://cdn.example.com/last.jpg");
        recipe.SetPrimaryImage(last.Id);

        recipe.RemoveImage(middle.Id);

        Assert.False(first.IsPrimary);
        Assert.True(last.IsPrimary);
        Assert.Equal(1, last.OrderIndex);
        Assert.Single(recipe.Images, image => image.IsPrimary);
    }

    [Fact]
    public void UnknownPrimaryDoesNotChangeExistingPrimary()
    {
        var recipe = CreateRecipe();
        var first = recipe.AddImage("https://cdn.example.com/first.jpg");

        Assert.Throws<DomainException>(() => recipe.SetPrimaryImage(Guid.NewGuid()));
        Assert.True(first.IsPrimary);
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
