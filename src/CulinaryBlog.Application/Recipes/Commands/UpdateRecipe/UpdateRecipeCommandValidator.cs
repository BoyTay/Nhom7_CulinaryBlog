using CulinaryBlog.Domain.Recipes;
using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(220);

        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(command => command.CategoryId)
            .NotEmpty();

        RuleFor(command => command.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.CookTimeMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.Servings)
            .GreaterThanOrEqualTo(1);

        RuleFor(command => command.Difficulty)
            .IsInEnum();

        RuleFor(command => command.Calories)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Calories.HasValue);

        RuleFor(command => command.ProteinGrams)
            .GreaterThanOrEqualTo(0)
            .When(command => command.ProteinGrams.HasValue);

        RuleFor(command => command.CarbohydratesGrams)
            .GreaterThanOrEqualTo(0)
            .When(command => command.CarbohydratesGrams.HasValue);

        RuleFor(command => command.FatGrams)
            .GreaterThanOrEqualTo(0)
            .When(command => command.FatGrams.HasValue);
    }
}
