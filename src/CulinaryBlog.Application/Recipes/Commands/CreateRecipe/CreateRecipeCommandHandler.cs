using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IDataSession dataSession)
    : ICommandHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = Recipe.Create(
            request.Title,
            request.Slug,
            request.Description,
            request.CategoryId,
            request.AuthorId,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Servings,
            request.Difficulty);

        recipeRepository.Add(recipe);

        await dataSession.SaveChangesAsync(cancellationToken);

        return recipe.Id;
    }
}