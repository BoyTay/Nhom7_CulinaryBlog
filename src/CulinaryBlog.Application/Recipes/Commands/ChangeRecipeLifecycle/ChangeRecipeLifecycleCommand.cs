using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Commands.ChangeRecipeLifecycle;

public sealed record ChangeRecipeLifecycleCommand(
    Guid RecipeId,
    RecipeLifecycleAction Action,
    uint Version) : ICommand;

public enum RecipeLifecycleAction
{
    Publish,
    Unpublish,
    Archive,
    Delete,
}
