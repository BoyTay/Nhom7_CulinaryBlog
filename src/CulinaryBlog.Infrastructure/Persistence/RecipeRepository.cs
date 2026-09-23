using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeRepository(ApplicationDbContext dbContext) : IRecipeRepository
{
    public Task<Recipe?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .SingleOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<Recipe?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .SingleOrDefaultAsync(recipe => recipe.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> ListPublishedAsync(
        CancellationToken cancellationToken = default) =>
        await QueryWithDetails()
            .Where(recipe => recipe.Status == RecipeStatus.Published)
            .OrderByDescending(recipe => recipe.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipe>> ListAsync(
        Guid? categoryId = null,
        RecipeDifficulty? difficulty = null,
        RecipeStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryWithDetails();

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
            .ToListAsync(cancellationToken);
    }

    public void Add(Recipe recipe) => dbContext.Recipes.Add(recipe);

    private IQueryable<Recipe> QueryWithDetails() => dbContext.Recipes
        .AsSplitQuery()
        .Include(recipe => recipe.Steps)
        .Include(recipe => recipe.Ingredients)
        .Include(recipe => recipe.Images);
}