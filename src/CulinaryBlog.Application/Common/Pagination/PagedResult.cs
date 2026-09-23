using System.Diagnostics.CodeAnalysis;

namespace CulinaryBlog.Application.Common.Pagination;

public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int TotalCount { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1 && TotalPages > 0;

    [SetsRequiredMembers]
    internal PagedResult(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        if (pageSize is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be between 1 and 50.");
        }

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}

public static class PagedResult
{
    public static PagedResult<T> Create<T>(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize) =>
        new(items, totalCount, page, pageSize);
}
