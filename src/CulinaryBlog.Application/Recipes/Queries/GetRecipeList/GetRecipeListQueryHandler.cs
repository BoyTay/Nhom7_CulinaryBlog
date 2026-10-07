using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeList;

public sealed class GetRecipeListQueryHandler(
    IRecipeRepository recipeRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetRecipeListQuery, IReadOnlyList<RecipeListItemDto>>
{
    public async Task<IReadOnlyList<RecipeListItemDto>> Handle(
        GetRecipeListQuery request,
        CancellationToken cancellationToken)
    {
        RecipeDifficulty? difficulty = null;
        RecipeStatus? status = null;

        if (!string.IsNullOrWhiteSpace(request.Difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(
                    request.Difficulty,
                    true,
                    out var parsedDifficulty))
            {
                throw new ArgumentException(
                    $"Invalid recipe difficulty: {request.Difficulty}");
            }

            difficulty = parsedDifficulty;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<RecipeStatus>(
                    request.Status,
                    true,
                    out var parsedStatus))
            {
                throw new ArgumentException(
                    $"Invalid recipe status: {request.Status}");
            }

            status = parsedStatus;
        }

        var isAdmin = currentUser.IsInRole(Roles.Admin);
        var authorId = currentUser.IsAuthenticated && currentUser.IsInRole(Roles.Author)
            ? currentUser.UserId
            : null;

        var recipes = await recipeRepository.ListAsync(
            request.CategoryId,
            difficulty,
            status,
            authorId,
            isAdmin,
            cancellationToken);

        return recipes
            .Select(recipe => new RecipeListItemDto(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.CategoryId,
                recipe.AuthorId,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty,
                recipe.Status,
                recipe.CreatedAt))
            .ToList();
    }
}
