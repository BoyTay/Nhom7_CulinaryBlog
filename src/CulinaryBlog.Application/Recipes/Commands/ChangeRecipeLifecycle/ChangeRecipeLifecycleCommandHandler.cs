using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.ChangeRecipeLifecycle;

public sealed class ChangeRecipeLifecycleCommandHandler(
    IRecipeRepository recipeRepository,
    IDataSession dataSession,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<ChangeRecipeLifecycleCommand>
{
    public async Task Handle(
        ChangeRecipeLifecycleCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(
            request.RecipeId,
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
                "You are not allowed to change this recipe.",
                "RECIPE_FORBIDDEN");
        }

        recipeRepository.SetOriginalVersion(recipe, request.Version);

        switch (request.Action)
        {
            case RecipeLifecycleAction.Publish:
                recipe.Publish();
                break;
            case RecipeLifecycleAction.Unpublish:
                recipe.Unpublish();
                break;
            case RecipeLifecycleAction.Archive:
                recipe.Archive();
                break;
            case RecipeLifecycleAction.Delete:
                recipe.Delete(timeProvider.GetUtcNow());
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Action,
                    "Unsupported recipe lifecycle action.");
        }

        await dataSession.SaveChangesAsync(cancellationToken);
    }
}
