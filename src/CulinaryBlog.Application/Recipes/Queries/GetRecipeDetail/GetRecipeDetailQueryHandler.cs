using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

public sealed class GetRecipeDetailQueryHandler(
    IRecipeReadRepository recipeReadRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetRecipeDetailQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(
        GetRecipeDetailQuery request,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUser.IsInRole(Roles.Admin);
        var authorId = currentUser.IsAuthenticated && currentUser.IsInRole(Roles.Author)
            ? currentUser.UserId
            : null;
        var detail = await recipeReadRepository.GetDetailAsync(
            request.Id,
            authorId,
            isAdmin,
            cancellationToken);

        if (detail is null)
        {
            throw new NotFoundException(
                "Recipe not found.",
                "RECIPE_NOT_FOUND");
        }

        return detail;
    }
}
