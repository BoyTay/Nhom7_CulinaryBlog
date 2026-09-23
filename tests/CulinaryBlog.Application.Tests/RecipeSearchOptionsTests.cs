using CulinaryBlog.Application.Abstractions.Search;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeSearchOptionsTests
{
    [Fact]
    public void ValidateAcceptsSupportedSortAndBoundaryValues()
    {
        var options = new RecipeSearchOptions { Sort = "-cookTime", PageSize = 50 };

        Assert.Same(options, options.Validate());
    }

    [Fact]
    public void ValidateNormalizesSearchTermAndSort()
    {
        var options = new RecipeSearchOptions
        {
            SearchTerm = "  soup  ",
            Sort = " relevance "
        };

        var validated = options.Validate();

        Assert.Equal("soup", validated.SearchTerm);
        Assert.Equal("relevance", validated.Sort);
    }

    [Fact]
    public void ValidateTreatsWhitespaceSearchTermAsMissing()
    {
        var options = new RecipeSearchOptions { SearchTerm = "   " };

        var validated = options.Validate();

        Assert.Null(validated.SearchTerm);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("  a  ")]
    public void ValidateRejectsMissingOrShortSearchTermForRelevanceSort(string? searchTerm)
    {
        var options = new RecipeSearchOptions { SearchTerm = searchTerm, Sort = "relevance" };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Theory]
    [InlineData("createdAt")]
    [InlineData("-createdAt")]
    [InlineData("title")]
    [InlineData("-title")]
    [InlineData("cookTime")]
    [InlineData("-cookTime")]
    [InlineData("relevance")]
    [InlineData("-relevance")]
    public void ValidateAcceptsEverySupportedSort(string sort)
    {
        var options = new RecipeSearchOptions
        {
            SearchTerm = sort.Contains("relevance", StringComparison.Ordinal) ? "soup" : null,
            Sort = sort
        };

        var validated = options.Validate();

        Assert.Equal(sort, validated.Sort);
    }

    [Fact]
    public void ValidateRejectsUnknownSort()
    {
        var options = new RecipeSearchOptions { Sort = "author" };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }
}
