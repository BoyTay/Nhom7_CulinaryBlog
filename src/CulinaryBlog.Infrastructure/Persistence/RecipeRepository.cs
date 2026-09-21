using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeRepository(ApplicationDbContext dbContext) : IRecipeRepository
{
    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .SingleOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .SingleOrDefaultAsync(recipe => recipe.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
        await QueryWithDetails()
            .Where(recipe => recipe.Status == RecipeStatus.Published)
            .OrderByDescending(recipe => recipe.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(Recipe recipe) => dbContext.Recipes.Add(recipe);

    private IQueryable<Recipe> QueryWithDetails() => dbContext.Recipes
        .Include(recipe => recipe.Steps)
        .Include(recipe => recipe.Ingredients)
        .Include(recipe => recipe.Images);
}