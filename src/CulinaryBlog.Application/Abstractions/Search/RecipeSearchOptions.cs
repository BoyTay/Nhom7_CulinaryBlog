namespace CulinaryBlog.Application.Abstractions.Search;

public sealed record RecipeSearchOptions
{
    public string? SearchTerm { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Difficulty { get; init; }

    public int? MaxCookTime { get; init; }

    public int? MinServings { get; init; }

    public string Sort { get; init; } = "-createdAt";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 12;

    public RecipeSearchOptions Validate()
    {
        if (Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Page), "Page must be at least 1.");
        }

        if (PageSize is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), "PageSize must be between 1 and 50.");
        }

        if (MaxCookTime is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxCookTime), "MaxCookTime cannot be negative.");
        }

        if (MinServings is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MinServings), "MinServings must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(Sort))
        {
            throw new ArgumentException("Sort cannot be empty.", nameof(Sort));
        }

        var sortField = Sort.StartsWith('-') ? Sort[1..] : Sort;
        if (sortField is not ("createdAt" or "title" or "cookTime" or "relevance"))
        {
            throw new ArgumentException("Sort must be createdAt, title, cookTime, or relevance.", nameof(Sort));
        }

        return this;
    }
}
