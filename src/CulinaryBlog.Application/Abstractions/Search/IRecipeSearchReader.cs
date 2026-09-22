using CulinaryBlog.Application.Common.Pagination;

namespace CulinaryBlog.Application.Abstractions.Search;

public interface IRecipeSearchReader
{
    Task<PagedResult<RecipeSearchResult>> SearchAsync(
        RecipeSearchOptions options,
        CancellationToken cancellationToken = default);
}

public sealed record RecipeSearchResult(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    Guid CategoryId,
    string Difficulty,
    int CookTime,
    DateTimeOffset PublishedAt,
    double? RelevanceScore = null);
