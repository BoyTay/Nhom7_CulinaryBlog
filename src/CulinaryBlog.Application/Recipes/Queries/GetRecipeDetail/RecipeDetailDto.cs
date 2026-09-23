using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

public sealed record RecipeDetailDto(
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
    RecipeNutritionDto? Nutrition,
    IReadOnlyList<RecipeStepDto> Steps,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeImageDto> Images,
    DateTimeOffset CreatedAt);

public sealed record RecipeNutritionDto(
    int Calories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams);

public sealed record RecipeStepDto(
    Guid Id,
    int StepNumber,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record RecipeIngredientDto(
    Guid Id,
    string Name,
    decimal Quantity,
    string Unit,
    string? Notes,
    int SortOrder);

public sealed record RecipeImageDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IsPrimary);
