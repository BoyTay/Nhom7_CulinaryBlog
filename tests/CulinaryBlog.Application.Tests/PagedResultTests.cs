using CulinaryBlog.Application.Common.Pagination;

namespace CulinaryBlog.Application.Tests;

public sealed class PagedResultTests
{
    [Fact]
    public void CreateCalculatesPageMetadata()
    {
        var result = PagedResult.Create(["one", "two"], 25, 2, 10);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void CreateRejectsPageSizeAboveApiLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult.Create<string>([], 0, 1, 51));
    }
}
