using CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;
using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeIngredient;

public sealed class ManageRecipeIngredientCommandValidator : AbstractValidator<ManageRecipeIngredientCommand>
{
    public ManageRecipeIngredientCommandValidator()
    {
        RuleFor(command => command.RecipeId).NotEmpty();
        RuleFor(command => command.Action).IsInEnum();
        RuleFor(command => command.IngredientId)
            .NotEmpty()
            .When(command => command.Action is RecipeChildAction.Update or RecipeChildAction.Delete);
        RuleFor(command => command.IngredientId)
            .Null()
            .When(command => command.Action == RecipeChildAction.Create);
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(command => command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.Quantity)
            .GreaterThan(0)
            .When(command => command.Quantity.HasValue
                && command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.Quantity)
            .NotNull()
            .When(command => command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.Unit)
            .NotEmpty()
            .MaximumLength(50)
            .When(command => command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.Notes)
            .MaximumLength(500)
            .When(command => command.Notes is not null
                && command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
    }
}
