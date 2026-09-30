using CulinaryBlog.Domain.Categories;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<int> CountRecipesAsync(Guid categoryId, bool publishedOnly, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
        Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
    void Add(Category category);
}

public sealed record CategoryRecipeSummary(Guid Id, string Title, string Slug, string Description, DateTimeOffset CreatedAt);
