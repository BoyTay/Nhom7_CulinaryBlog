using CulinaryBlog.Application.Abstractions.Search;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeSearchOptionsTests
{
    [Fact]
    public void ValidateAcceptsSupportedDescendingSort()
    {
        var options = new RecipeSearchOptions { Sort = "-cookTime", PageSize = 50 };

        Assert.Same(options, options.Validate());
    }

    [Fact]
    public void ValidateRejectsUnknownSort()
    {
        var options = new RecipeSearchOptions { Sort = "author" };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }
}
