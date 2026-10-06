using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeSearchReader(ApplicationDbContext dbContext) : IRecipeSearchReader
{
    public async Task<PagedResult<RecipeSearchResult>> SearchAsync(
        RecipeSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options = options.Validate();

        var query = dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published);

        if (options.CategoryId is { } categoryId)
        {
            query = query.Where(recipe => recipe.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(options.Difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(options.Difficulty, true, out var difficulty) ||
                !Enum.IsDefined(difficulty))
            {
                throw new ArgumentException(
                    $"Unsupported recipe difficulty '{options.Difficulty}'.",
                    nameof(options));
            }

            query = query.Where(recipe => recipe.Difficulty == difficulty);
        }

        if (options.MaxCookTime is { } maxCookTime)
        {
            query = query.Where(recipe => recipe.CookTimeMinutes <= maxCookTime);
        }

        if (options.MinServings is { } minServings)
        {
            query = query.Where(recipe => recipe.Servings >= minServings);
        }

        var normalizedSearchTerm = options.SearchTerm;
        if (normalizedSearchTerm is not null)
        {
            var searchPattern = $"%{EscapeLikePattern(normalizedSearchTerm)}%";
            query = query.Where(recipe =>
                EF.Functions.ILike(recipe.Title, searchPattern, "\\") ||
                EF.Functions.ILike(recipe.Description, searchPattern, "\\"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = ApplySort(query, options.Sort, normalizedSearchTerm);
        var offset = (int)Math.Min(((long)options.Page - 1) * options.PageSize, int.MaxValue);
        var recipes = await orderedQuery
            .Skip(offset)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        var results = recipes
            .Select(recipe => new RecipeSearchResult(
                recipe.Id,
                recipe.Slug,
                recipe.Title,
                recipe.Description,
                recipe.CategoryId,
                recipe.Difficulty.ToString(),
                recipe.CookTimeMinutes,
                recipe.PublishedAt ?? recipe.CreatedAt,
                GetRelevanceScore(recipe, normalizedSearchTerm)))
            .ToList();

        return PagedResult.Create(results, totalCount, options.Page, options.PageSize);
    }

    private static IQueryable<Recipe> ApplySort(
        IQueryable<Recipe> query,
        string sort,
        string? normalizedSearchTerm)
    {
        var descending = sort.StartsWith('-');
        var sortField = descending ? sort[1..] : sort;

        return sortField switch
        {
            "createdAt" => descending
                ? query.OrderByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id)
                : query.OrderBy(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "title" => descending
                ? query.OrderByDescending(recipe => recipe.Title).ThenBy(recipe => recipe.Id)
                : query.OrderBy(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "cookTime" => descending
                ? query.OrderByDescending(recipe => recipe.CookTimeMinutes).ThenBy(recipe => recipe.Id)
                : query.OrderBy(recipe => recipe.CookTimeMinutes).ThenBy(recipe => recipe.Id),
            "relevance" => ApplyRelevanceSort(query, normalizedSearchTerm!, !descending),
            _ => throw new ArgumentException($"Unsupported sort field '{sort}'.", nameof(sort)),
        };
    }

    private static IQueryable<Recipe> ApplyRelevanceSort(
        IQueryable<Recipe> query,
        string normalizedSearchTerm,
        bool ascending)
    {
        var exactTitle = query.Select(recipe => new
        {
            Recipe = recipe,
            IsExactTitle = EF.Functions.ILike(
                recipe.Title,
                EscapeLikePattern(normalizedSearchTerm),
                "\\"),
            StartsWithTitle = EF.Functions.ILike(
                recipe.Title,
                $"{EscapeLikePattern(normalizedSearchTerm)}%",
                "\\"),
            ContainsTitle = EF.Functions.ILike(
                recipe.Title,
                $"%{EscapeLikePattern(normalizedSearchTerm)}%",
                "\\"),
            ContainsDescription = EF.Functions.ILike(
                recipe.Description,
                $"%{EscapeLikePattern(normalizedSearchTerm)}%",
                "\\"),
        });

        var ordered = ascending
            ? exactTitle
                .OrderBy(item => item.IsExactTitle)
                .ThenBy(item => item.StartsWithTitle)
                .ThenBy(item => item.ContainsTitle)
                .ThenBy(item => item.ContainsDescription)
            : exactTitle
                .OrderByDescending(item => item.IsExactTitle)
                .ThenByDescending(item => item.StartsWithTitle)
                .ThenByDescending(item => item.ContainsTitle)
                .ThenByDescending(item => item.ContainsDescription);

        return ordered
            .ThenByDescending(item => item.Recipe.CreatedAt)
            .ThenBy(item => item.Recipe.Id)
            .Select(item => item.Recipe);
    }

    private static double? GetRelevanceScore(Recipe recipe, string? normalizedSearchTerm)
    {
        if (normalizedSearchTerm is null)
        {
            return null;
        }

        if (string.Equals(recipe.Title, normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (recipe.Title.StartsWith(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 0.8;
        }

        if (recipe.Title.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 0.6;
        }

        return 0.4;
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
