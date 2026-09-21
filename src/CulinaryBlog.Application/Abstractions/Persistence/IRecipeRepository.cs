using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListPublishedAsync(CancellationToken cancellationToken = default);

    void Add(Recipe recipe);
}
