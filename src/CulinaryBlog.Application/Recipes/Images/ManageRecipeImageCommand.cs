using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed record ManageRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    RecipeImageAction Action,
    uint Version) : ICommand<Guid>;

public enum RecipeImageAction
{
    SetPrimary,
    Delete,
}
