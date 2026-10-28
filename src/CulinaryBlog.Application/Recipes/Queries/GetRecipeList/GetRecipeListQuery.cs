using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeList;

public sealed record GetRecipeListQuery(
    Guid? CategoryId = null,
    string? Difficulty = null,
    string? Status = null,
    bool Mine = false)
    : IQuery<IReadOnlyList<RecipeListItemDto>>;
