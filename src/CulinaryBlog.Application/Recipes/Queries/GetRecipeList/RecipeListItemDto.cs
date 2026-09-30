using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeList;

public sealed record RecipeListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    string AuthorId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    RecipeStatus Status,
    DateTimeOffset CreatedAt);
