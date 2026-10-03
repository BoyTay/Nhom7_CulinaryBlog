using CulinaryBlog.Domain.Categories;

namespace CulinaryBlog.Application.Tests;

public sealed class CategoryTests
{
    [Fact]
    public void CreateNormalizesCategoryValues()
    {
        var category = Category.Create("  Món chính  ", "  mon-chinh  ", "  Bữa ăn chính  ", "  https://cdn.example.com/main.jpg  ", 2);
        Assert.Equal("Món chính", category.Name);
        Assert.Equal("mon-chinh", category.Slug);
        Assert.Equal("Bữa ăn chính", category.Description);
        Assert.Equal("https://cdn.example.com/main.jpg", category.ImageUrl);
        Assert.Equal(2, category.OrderIndex);
    }

    [Theory]
    [InlineData("", "mon-chinh")]
    [InlineData("Món chính", "")]
    public void CreateRejectsMissingRequiredValues(string name, string slug) =>
        Assert.Throws<ArgumentException>(() => Category.Create(name, slug));

    [Fact]
    public void UpdatePreservesSlugForStableUrls()
    {
        var category = Category.Create("Món chính", "mon-chinh");
        category.Update("Món chính Việt", "Món ăn truyền thống", null, 1);
        Assert.Equal("mon-chinh", category.Slug);
        Assert.Equal("Món chính Việt", category.Name);
    }

    [Fact]
    public void CreateRejectsNegativeOrderIndex() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Category.Create("Món chính", "mon-chinh", orderIndex: -1));
}
