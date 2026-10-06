using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Domain.Recipes;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeSearchEndpoints
{
    public static RouteGroupBuilder MapRecipeSearchEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/recipes/search", async Task<IResult> (
            IRecipeSearchReader searchReader,
            CancellationToken cancellationToken,
            string? q,
            Guid? categoryId,
            string? difficulty,
            int? maxCookTime,
            int? minServings,
            string? sort,
            int? page,
            int? pageSize) =>
        {
            var options = new RecipeSearchOptions
            {
                SearchTerm = q,
                CategoryId = categoryId,
                Difficulty = difficulty,
                MaxCookTime = maxCookTime,
                MinServings = minServings,
                Sort = sort ?? "-createdAt",
                Page = page ?? 1,
                PageSize = pageSize ?? 12,
            };

            try
            {
                options = options.Validate();
                if (options.Difficulty is { } difficultyValue &&
                    (!Enum.TryParse<RecipeDifficulty>(difficultyValue, true, out var parsedDifficulty) ||
                     !Enum.IsDefined(parsedDifficulty)))
                {
                    throw new ArgumentException(
                        $"Unsupported recipe difficulty '{difficultyValue}'.",
                        nameof(difficulty));
                }

                var result = await searchReader.SearchAsync(options, cancellationToken);
                return Results.Ok(result);
            }
            catch (ArgumentException exception)
            {
                var field = exception.ParamName switch
                {
                    nameof(RecipeSearchOptions.SearchTerm) => "q",
                    nameof(RecipeSearchOptions.CategoryId) => "categoryId",
                    nameof(RecipeSearchOptions.Difficulty) => "difficulty",
                    nameof(RecipeSearchOptions.MaxCookTime) => "maxCookTime",
                    nameof(RecipeSearchOptions.MinServings) => "minServings",
                    nameof(difficulty) => "difficulty",
                    nameof(RecipeSearchOptions.Page) => "page",
                    nameof(RecipeSearchOptions.PageSize) => "pageSize",
                    nameof(RecipeSearchOptions.Sort) => "sort",
                    nameof(options) when exception.Message.StartsWith(
                        "Unsupported recipe difficulty",
                        StringComparison.Ordinal) => "difficulty",
                    _ => "search",
                };

                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [field] = [exception.Message] },
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
        })
        .WithName("SearchRecipes")
        .WithTags("Recipes")
        .WithSummary("Search published recipes using Vietnamese full-text and typo-tolerant matching")
        .Produces<PagedResult<RecipeSearchResult>>()
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();

        return api;
    }
}
