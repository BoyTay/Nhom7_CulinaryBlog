using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(category => category.OrderIndex).ThenBy(category => category.Name).ToListAsync(cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);
}
