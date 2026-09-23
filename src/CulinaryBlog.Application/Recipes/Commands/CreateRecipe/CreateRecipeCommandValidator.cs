using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
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

        RuleFor(command => command.AuthorId)
            .NotEmpty()
            .MaximumLength(450);

        RuleFor(command => command.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.CookTimeMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.Servings)
            .GreaterThanOrEqualTo(1);

        RuleFor(command => command.Difficulty)
            .IsInEnum();
    }
}