using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Common.Pagination;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories;

public static class CategoryQueries
{
    public const string CacheKey = "categories:all";
}

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesQueryHandler(ICategoryRepository categories, IMemoryCache cache)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CategoryQueries.CacheKey, out IReadOnlyList<CategoryDto>? cached) && cached is not null)
        {
            return cached;
        }

        var all = await categories.ListAsync(cancellationToken);
        var results = new List<CategoryDto>(all.Count);
        foreach (var category in all.OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase))
        {
            var recipeCount = await categories.CountRecipesAsync(category.Id, true, cancellationToken);
            results.Add(CategoryDto.From(category, recipeCount));
        }

        cache.Set(CategoryQueries.CacheKey, results, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(60),
        });
        return results;
    }
}

public sealed record GetCategoryBySlugQuery(string Slug, int Page = 1, int PageSize = 12)
    : IQuery<CategoryDetailDto>;

public sealed record CategoryDetailDto(CategoryDto Category, PagedResult<CategoryRecipeSummary> Recipes);

public sealed class GetCategoryBySlugQueryValidator : AbstractValidator<GetCategoryBySlugQuery>
{
    public GetCategoryBySlugQueryValidator()
    {
        RuleFor(query => query.Slug).NotEmpty().MaximumLength(120);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetCategoryBySlugQueryHandler(ICategoryRepository categories, ICurrentUser currentUser)
    : IQueryHandler<GetCategoryBySlugQuery, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await categories.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Category not found.", "CATEGORY_NOT_FOUND");
        var authorId = currentUser.IsAuthenticated ? currentUser.UserId : null;
        var (items, totalCount) = await categories.ListRecipesAsync(
            category.Id, authorId, request.Page, request.PageSize, cancellationToken);
        var publishedCount = await categories.CountRecipesAsync(category.Id, true, cancellationToken);
        return new CategoryDetailDto(
            CategoryDto.From(category, publishedCount),
            PagedResult.Create(items, totalCount, request.Page, request.PageSize));
    }
}
