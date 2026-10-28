using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeReadRepository(ApplicationDbContext dbContext) : IRecipeReadRepository
{
    public Task<RecipeDetailDto?> GetDetailAsync(
        Guid id,
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default) =>
        QueryDetail(
            VisibleRecipes(authorId, includeAllStatuses).Where(recipe => recipe.Id == id),
            cancellationToken);

    public Task<RecipeDetailDto?> GetDetailBySlugAsync(
        string slug,
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default) =>
        QueryDetail(
            VisibleRecipes(authorId, includeAllStatuses).Where(recipe => recipe.Slug == slug),
            cancellationToken);

    private static Task<RecipeDetailDto?> QueryDetail(
        IQueryable<Recipe> query,
        CancellationToken cancellationToken)
    {
        return query
            .AsSplitQuery()
            .Select(recipe => new RecipeDetailDto(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.CategoryId,
                recipe.AuthorId,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty,
                recipe.Status,
                recipe.PublishedAt,
                recipe.Version,
                recipe.Nutrition == null
                    ? null
                    : new RecipeNutritionDto(
                        recipe.Nutrition.Calories,
                        recipe.Nutrition.ProteinGrams,
                        recipe.Nutrition.CarbohydratesGrams,
                        recipe.Nutrition.FatGrams),
                recipe.Steps
                    .OrderBy(step => step.StepNumber)
                    .Select(step => new RecipeStepDto(
                        step.Id,
                        step.StepNumber,
                        step.Description,
                        step.TimerMinutes,
                        step.ImageUrl))
                    .ToList(),
                recipe.Ingredients
                    .OrderBy(ingredient => ingredient.SortOrder)
                    .Select(ingredient => new RecipeIngredientDto(
                        ingredient.Id,
                        ingredient.Name,
                        ingredient.Quantity,
                        ingredient.Unit,
                        ingredient.Notes,
                        ingredient.SortOrder))
                    .ToList(),
                recipe.Images
                    .OrderBy(image => image.OrderIndex)
                    .Select(image => new RecipeImageDto(
                        image.Id,
                        image.Url,
                        image.OriginalUrl,
                        image.MediumUrl,
                        image.ThumbnailUrl,
                        image.AltText,
                        image.IsPrimary,
                        image.OrderIndex))
                    .ToList(),
                recipe.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeListItemDto>> ListAsync(
        Guid? categoryId = null,
        RecipeDifficulty? difficulty = null,
        RecipeStatus? status = null,
        string? authorId = null,
        bool includeAllStatuses = false,
        bool ownOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = ownOnly
            ? dbContext.Recipes.AsNoTracking().Where(recipe => recipe.AuthorId == authorId)
            : VisibleRecipes(authorId, includeAllStatuses);

        if (categoryId.HasValue)
        {
            query = query.Where(recipe => recipe.CategoryId == categoryId.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(recipe => recipe.Difficulty == difficulty.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(recipe => recipe.Status == status.Value);
        }

        return await query
            .OrderByDescending(recipe => recipe.CreatedAt)
            .Select(recipe => new RecipeListItemDto(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.CategoryId,
                recipe.AuthorId,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty,
                recipe.Status,
                recipe.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Recipe> VisibleRecipes(string? authorId, bool includeAllStatuses)
    {
        var query = dbContext.Recipes.AsNoTracking();
        if (!includeAllStatuses)
        {
            query = query.Where(recipe => recipe.Status == RecipeStatus.Published
                || (authorId != null && recipe.AuthorId == authorId));
        }

        return query;
    }
}
