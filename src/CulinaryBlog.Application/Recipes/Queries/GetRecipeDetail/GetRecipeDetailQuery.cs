using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

public sealed record GetRecipeDetailQuery(Guid Id)
    : IQuery<RecipeDetailDto>;
