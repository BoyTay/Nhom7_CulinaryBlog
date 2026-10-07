using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Recipe?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListPublishedAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListAsync(
        Guid? categoryId = null,
        RecipeDifficulty? difficulty = null,
        RecipeStatus? status = null,
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default);

    void Add(Recipe recipe);

    void SetOriginalVersion(Recipe recipe, uint version);
}
