using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IDataSession dataSession,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateRecipeCommand>
{
    public async Task Handle(
        UpdateRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                "Recipe not found.",
                "RECIPE_NOT_FOUND");

        var isAdmin = currentUser.IsInRole(Roles.Admin);
        var isOwner = currentUser.IsAuthenticated
                      && currentUser.IsInRole(Roles.Author)
                      && !string.IsNullOrWhiteSpace(currentUser.UserId)
                      && recipe.AuthorId == currentUser.UserId;

        if (!isAdmin && !isOwner)
        {
            throw new ForbiddenException(
                "You are not allowed to update this recipe.",
                "RECIPE_FORBIDDEN");
        }

        RecipeNutrition? nutrition = null;

        if (request.Calories.HasValue
            || request.ProteinGrams.HasValue
            || request.CarbohydratesGrams.HasValue
            || request.FatGrams.HasValue)
        {
            nutrition = RecipeNutrition.Create(
                request.Calories ?? 0,
                request.ProteinGrams ?? 0,
                request.CarbohydratesGrams ?? 0,
                request.FatGrams ?? 0);
        }

        recipeRepository.SetOriginalVersion(recipe, request.Version);

        recipe.Update(
            request.Title,
            request.Slug,
            request.Description,
            request.CategoryId,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Servings,
            request.Difficulty,
            nutrition);

        await dataSession.SaveChangesAsync(cancellationToken);
    }
}
