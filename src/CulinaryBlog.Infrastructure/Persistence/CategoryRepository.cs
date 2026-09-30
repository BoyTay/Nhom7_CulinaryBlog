using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var pattern = name.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return dbContext.Categories.IgnoreQueryFilters().AnyAsync(
            category => (excludeId == null || category.Id != excludeId) && EF.Functions.ILike(category.Name, pattern, "\\"),
            cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Categories.IgnoreQueryFilters().AnyAsync(category => category.Slug == slug, cancellationToken);

    public Task<int> CountRecipesAsync(Guid categoryId, bool publishedOnly, CancellationToken cancellationToken = default) =>
        dbContext.Recipes.CountAsync(
            recipe => recipe.CategoryId == categoryId && (!publishedOnly || recipe.Status == RecipeStatus.Published),
            cancellationToken);

    public async Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
        Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recipes.AsNoTracking().Where(recipe =>
            recipe.CategoryId == categoryId &&
            (recipe.Status == RecipeStatus.Published ||
             (authorId != null && recipe.Status == RecipeStatus.Draft && recipe.AuthorId == authorId)));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(recipe => recipe.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(recipe => new CategoryRecipeSummary(recipe.Id, recipe.Title, recipe.Slug, recipe.Description, recipe.CreatedAt))
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(category => category.OrderIndex).ThenBy(category => category.Name).ToListAsync(cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);
}
