using CulinaryBlog.Application.Recipes.Commands.ManageRecipeIngredient;
using CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;
using FluentValidation.Results;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeNestedResourceValidatorTests
{
    private static readonly Guid RecipeId = Guid.NewGuid();

    [Fact]
    public void StepValidatorRejectsMissingDescription()
    {
        var result = new ManageRecipeStepCommandValidator().Validate(
            new ManageRecipeStepCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                " ",
                null,
                null,
                1));

        AssertHasError(result, nameof(ManageRecipeStepCommand.Description));
    }

    [Fact]
    public void StepValidatorRejectsDescriptionOver2000Characters()
    {
        var result = new ManageRecipeStepCommandValidator().Validate(
            new ManageRecipeStepCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                new string('x', 2001),
                null,
                null,
                1));

        AssertHasError(result, nameof(ManageRecipeStepCommand.Description));
    }

    [Fact]
    public void StepValidatorRejectsNegativeTimer()
    {
        var result = new ManageRecipeStepCommandValidator().Validate(
            new ManageRecipeStepCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                "Mix the ingredients.",
                -1,
                null,
                1));

        AssertHasError(result, nameof(ManageRecipeStepCommand.TimerMinutes));
    }

    [Fact]
    public void StepValidatorRejectsImageUrlOver2048Characters()
    {
        var result = new ManageRecipeStepCommandValidator().Validate(
            new ManageRecipeStepCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                "Mix the ingredients.",
                null,
                new string('x', 2049),
                1));

        AssertHasError(result, nameof(ManageRecipeStepCommand.ImageUrl));
    }

    [Fact]
    public void IngredientValidatorRejectsMissingNameUnitAndQuantity()
    {
        var result = new ManageRecipeIngredientCommandValidator().Validate(
            new ManageRecipeIngredientCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                " ",
                null,
                "",
                null,
                1));

        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Name));
        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Quantity));
        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Unit));
    }

    [Fact]
    public void IngredientValidatorRejectsNameAndUnitOverTheirLimits()
    {
        var result = new ManageRecipeIngredientCommandValidator().Validate(
            new ManageRecipeIngredientCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                new string('n', 201),
                1,
                new string('u', 51),
                null,
                1));

        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Name));
        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Unit));
    }

    [Fact]
    public void IngredientValidatorRejectsNotesOver500Characters()
    {
        var result = new ManageRecipeIngredientCommandValidator().Validate(
            new ManageRecipeIngredientCommand(
                RecipeId,
                null,
                RecipeChildAction.Create,
                "Flour",
                100,
                "g",
                new string('n', 501),
                1));

        AssertHasError(result, nameof(ManageRecipeIngredientCommand.Notes));
    }

    [Fact]
    public void IngredientValidatorRejectsZeroAndNegativeQuantity()
    {
        var validator = new ManageRecipeIngredientCommandValidator();
        foreach (var quantity in new decimal[] { 0, -1 })
        {
            var result = validator.Validate(
                new ManageRecipeIngredientCommand(
                    RecipeId,
                    null,
                    RecipeChildAction.Create,
                    "Flour",
                    quantity,
                    "g",
                    null,
                    1));

            AssertHasError(result, nameof(ManageRecipeIngredientCommand.Quantity));
        }
    }

    private static void AssertHasError(ValidationResult result, string propertyName) =>
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
}
