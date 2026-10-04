using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.UpdateRecipe;

public sealed record UpdateRecipeCommand(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    int? Calories,
    decimal? ProteinGrams,
    decimal? CarbohydratesGrams,
    decimal? FatGrams,
    uint Version) : ICommand;
