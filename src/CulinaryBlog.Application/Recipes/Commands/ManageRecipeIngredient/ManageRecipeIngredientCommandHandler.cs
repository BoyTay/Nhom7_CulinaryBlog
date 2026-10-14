using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeIngredient;

public sealed class ManageRecipeIngredientCommandHandler(
    IRecipeRepository recipeRepository,
    IDataSession dataSession,
    IRecipeAuthorizationService recipeAuthorization,
    TimeProvider timeProvider)
    : ICommandHandler<ManageRecipeIngredientCommand, Guid>
{
    public async Task<Guid> Handle(
        ManageRecipeIngredientCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.", "RECIPE_NOT_FOUND");

        recipeAuthorization.EnsureCanManage(recipe);

        if (request.Action != RecipeChildAction.Create
            && !recipe.Ingredients.Any(ingredient => ingredient.Id == request.IngredientId))
        {
            throw new NotFoundException("Recipe ingredient not found.", "INGREDIENT_NOT_FOUND");
        }

        recipeRepository.SetOriginalVersion(recipe, request.Version);
        recipe.MarkUpdated(timeProvider.GetUtcNow());

        var ingredientId = request.Action switch
        {
            RecipeChildAction.Create => AddIngredient(recipe, request),
            RecipeChildAction.Update => UpdateIngredient(recipe, request),
            RecipeChildAction.Delete => DeleteIngredient(recipe, request.IngredientId!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unsupported recipe child action."),
        };

        await dataSession.SaveChangesAsync(cancellationToken);
        return ingredientId;
    }

    private static Guid UpdateIngredient(Recipe recipe, ManageRecipeIngredientCommand request)
    {
        recipe.UpdateIngredient(
            request.IngredientId!.Value,
            request.Name!,
            request.Quantity!.Value,
            request.Unit!,
            request.Notes);
        return request.IngredientId.Value;
    }

    private Guid AddIngredient(Recipe recipe, ManageRecipeIngredientCommand request)
    {
        var ingredient = recipe.AddIngredient(
            request.Name!,
            request.Quantity!.Value,
            request.Unit!,
            request.Notes);
        recipeRepository.AddIngredient(ingredient);
        return ingredient.Id;
    }

    private static Guid DeleteIngredient(Recipe recipe, Guid ingredientId)
    {
        recipe.RemoveIngredient(ingredientId);
        return ingredientId;
    }
}
