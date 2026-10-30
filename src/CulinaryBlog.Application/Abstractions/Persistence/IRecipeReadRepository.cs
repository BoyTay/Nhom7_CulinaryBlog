using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRecipeReadRepository
{
    Task<RecipeDetailDto?> GetDetailAsync(
        Guid id,
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default);

    Task<RecipeDetailDto?> GetDetailBySlugAsync(
        string slug,
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecipeListItemDto>> ListAsync(
        Guid? categoryId = null,
        RecipeDifficulty? difficulty = null,
        RecipeStatus? status = null,
        string? authorId = null,
        bool includeAllStatuses = false,
        bool ownOnly = false,
        CancellationToken cancellationToken = default);
}
