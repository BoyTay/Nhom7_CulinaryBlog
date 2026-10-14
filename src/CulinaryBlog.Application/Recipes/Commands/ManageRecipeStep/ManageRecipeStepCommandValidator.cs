using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;

public sealed class ManageRecipeStepCommandValidator : AbstractValidator<ManageRecipeStepCommand>
{
    public ManageRecipeStepCommandValidator()
    {
        RuleFor(command => command.RecipeId).NotEmpty();
        RuleFor(command => command.Action).IsInEnum();
        RuleFor(command => command.StepId)
            .NotEmpty()
            .When(command => command.Action is RecipeChildAction.Update or RecipeChildAction.Delete);
        RuleFor(command => command.StepId)
            .Null()
            .When(command => command.Action == RecipeChildAction.Create);
        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(2000)
            .When(command => command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.TimerMinutes)
            .GreaterThanOrEqualTo(0)
            .When(command => command.TimerMinutes.HasValue
                && command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
        RuleFor(command => command.ImageUrl)
            .MaximumLength(2048)
            .When(command => command.ImageUrl is not null
                && command.Action is RecipeChildAction.Create or RecipeChildAction.Update);
    }
}
