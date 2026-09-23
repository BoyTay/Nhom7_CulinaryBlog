using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.CreateRecipe;

public sealed record CreateRecipeCommand(
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    string AuthorId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty) : ICommand<Guid>;