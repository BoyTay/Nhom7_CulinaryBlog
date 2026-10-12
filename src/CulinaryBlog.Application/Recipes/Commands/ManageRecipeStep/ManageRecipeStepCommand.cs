using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;

public sealed record ManageRecipeStepCommand(
    Guid RecipeId,
    Guid? StepId,
    RecipeChildAction Action,
    string? Description,
    int? TimerMinutes,
    string? ImageUrl,
    uint Version) : ICommand<Guid>;

public enum RecipeChildAction
{
    Create,
    Update,
    Delete,
}
