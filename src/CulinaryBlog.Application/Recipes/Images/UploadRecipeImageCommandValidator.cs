using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class UploadRecipeImageCommandValidator : AbstractValidator<UploadRecipeImageCommand>
{
    public UploadRecipeImageCommandValidator()
    {
        RuleFor(command => command.RecipeId).NotEmpty();
        RuleFor(command => command.Content).NotNull();
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.AltText).MaximumLength(200);
    }
}
