using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeIngredient;

public sealed record ManageRecipeIngredientCommand(
    Guid RecipeId,
    Guid? IngredientId,
    RecipeChildAction Action,
    string? Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    uint Version) : ICommand<Guid>;
