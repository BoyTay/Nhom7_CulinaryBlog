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
        string? authorId = null,
        bool includeAllStatuses = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recipes.AsNoTracking();

        if (!includeAllStatuses)
        {
            query = query.Where(recipe => recipe.Status == RecipeStatus.Published
                || (authorId != null && recipe.AuthorId == authorId));
        }

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

    public void AddStep(RecipeStep recipeStep) => dbContext.RecipeSteps.Add(recipeStep);

    public void AddIngredient(RecipeIngredient recipeIngredient) => dbContext.RecipeIngredients.Add(recipeIngredient);

    public void AddImage(RecipeImage recipeImage) => dbContext.RecipeImages.Add(recipeImage);

    public async Task SaveImageChangesAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var expectedVersion = dbContext.Entry(recipe).Property(item => item.Version).OriginalValue;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // EF may insert a child before updating its parent. Lock and check xmin first
        // so competing image writes report a version conflict, not a unique-index error.
        var versions = await dbContext.Database.SqlQuery<long>(
            $"""SELECT xmin::text::bigint AS "Value" FROM "Recipes" WHERE "Id" = {recipe.Id} AND NOT "IsDeleted" FOR UPDATE""")
            .ToListAsync(cancellationToken);
        if (versions.Count != 1 || versions[0] != expectedVersion)
        {
            throw new DbUpdateConcurrencyException("The recipe changed before its images could be saved.");
        }

        // Image-only writes must also advance the aggregate's xmin.
        dbContext.Entry(recipe).Property(item => item.UpdatedAt).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public void SetOriginalVersion(Recipe recipe, uint version) =>
        dbContext.Entry(recipe).Property(r => r.Version).OriginalValue = version;

    private IQueryable<Recipe> QueryWithDetails() => dbContext.Recipes
        .AsSplitQuery()
        .Include(recipe => recipe.Steps)
        .Include(recipe => recipe.Ingredients)
        .Include(recipe => recipe.Images);
}
