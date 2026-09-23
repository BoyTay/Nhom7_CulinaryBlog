using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

public sealed class GetRecipeDetailQueryHandler(
    IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecipeDetailQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(
        GetRecipeDetailQuery request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (recipe is null)
        {
            throw new KeyNotFoundException(
                $"Recipe with id '{request.Id}' was not found.");
        }

        var nutrition = recipe.Nutrition is null
            ? null
            : new RecipeNutritionDto(
                recipe.Nutrition.Calories,
                recipe.Nutrition.ProteinGrams,
                recipe.Nutrition.CarbohydratesGrams,
                recipe.Nutrition.FatGrams);

        return new RecipeDetailDto(
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
            nutrition,
            recipe.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new RecipeStepDto(
                    step.Id,
                    step.StepNumber,
                    step.Description,
                    step.TimerMinutes,
                    step.ImageUrl))
                .ToList(),
            recipe.Ingredients
                .OrderBy(ingredient => ingredient.SortOrder)
                .Select(ingredient => new RecipeIngredientDto(
                    ingredient.Id,
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Notes,
                    ingredient.SortOrder))
                .ToList(),
            recipe.Images
                .Select(image => new RecipeImageDto(
                    image.Id,
                    image.Url,
                    image.AltText,
                    image.IsPrimary))
                .ToList(),
            recipe.CreatedAt);
    }
}
