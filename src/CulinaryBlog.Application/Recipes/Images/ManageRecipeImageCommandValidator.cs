using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class ManageRecipeImageCommandValidator : AbstractValidator<ManageRecipeImageCommand>
{
    public ManageRecipeImageCommandValidator()
    {
        RuleFor(command => command.RecipeId).NotEmpty();
        RuleFor(command => command.ImageId).NotEmpty();
        RuleFor(command => command.Action).IsInEnum();
    }
}
