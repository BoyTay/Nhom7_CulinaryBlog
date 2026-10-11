using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;

public sealed class ManageRecipeStepCommandHandler(
    IRecipeRepository recipeRepository,
    IDataSession dataSession,
    IRecipeAuthorizationService recipeAuthorization,
    TimeProvider timeProvider)
    : ICommandHandler<ManageRecipeStepCommand, Guid>
{
    public async Task<Guid> Handle(
        ManageRecipeStepCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.", "RECIPE_NOT_FOUND");

        recipeAuthorization.EnsureCanManage(recipe);

        if (request.Action != RecipeChildAction.Create
            && !recipe.Steps.Any(step => step.Id == request.StepId))
        {
            throw new NotFoundException("Recipe step not found.", "STEP_NOT_FOUND");
        }

        recipeRepository.SetOriginalVersion(recipe, request.Version);
        recipe.MarkUpdated(timeProvider.GetUtcNow());

        var stepId = request.Action switch
        {
            RecipeChildAction.Create => AddStep(recipe, request),
            RecipeChildAction.Update => UpdateStep(recipe, request),
            RecipeChildAction.Delete => DeleteStep(recipe, request.StepId!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unsupported recipe child action."),
        };

        await dataSession.SaveChangesAsync(cancellationToken);
        return stepId;
    }

    private static Guid UpdateStep(Recipe recipe, ManageRecipeStepCommand request)
    {
        recipe.UpdateStep(
            request.StepId!.Value,
            request.Description!,
            request.TimerMinutes,
            request.ImageUrl);
        return request.StepId.Value;
    }

    private Guid AddStep(Recipe recipe, ManageRecipeStepCommand request)
    {
        var step = recipe.AddStep(request.Description!, request.TimerMinutes, request.ImageUrl);
        recipeRepository.AddStep(step);
        return step.Id;
    }

    private static Guid DeleteStep(Recipe recipe, Guid stepId)
    {
        recipe.RemoveStep(stepId);
        return stepId;
    }
}
