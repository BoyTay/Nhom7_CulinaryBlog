using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeBySlug;

public sealed class GetRecipeBySlugQueryHandler(
    IRecipeReadRepository recipeReadRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetRecipeBySlugQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(
        GetRecipeBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUser.IsInRole(Roles.Admin);
        var authorId = currentUser.IsAuthenticated && currentUser.IsInRole(Roles.Author)
            ? currentUser.UserId
            : null;

        return await recipeReadRepository.GetDetailBySlugAsync(
                request.Slug,
                authorId,
                isAdmin,
                cancellationToken)
            ?? throw new NotFoundException("Recipe not found.", "RECIPE_NOT_FOUND");
    }
}
