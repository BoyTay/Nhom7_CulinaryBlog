using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeBySlug;

public sealed record GetRecipeBySlugQuery(string Slug)
    : IQuery<RecipeDetailDto>;
