using CulinaryBlog.Domain.Categories;

namespace CulinaryBlog.Application.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    int RecipeCount)
{
    public static CategoryDto From(Category category, int recipeCount = 0) =>
        new(category.Id, category.Name, category.Slug, category.Description, category.ImageUrl, category.OrderIndex, recipeCount);
}
