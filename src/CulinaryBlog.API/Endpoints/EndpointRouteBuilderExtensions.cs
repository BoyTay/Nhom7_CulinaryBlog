using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Recipes.Commands.CreateRecipe;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
using CulinaryBlog.Domain.Recipes;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        api.MapGet("/", () => Results.Ok(new
        {
            name = "Culinary Blog API",
            version = "v1",
        }))
        .WithName("GetApiInformation")
        .WithTags("System");

        api.MapGet("/recipes", async (
            Guid? categoryId,
            string? difficulty,
            string? status,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRecipeListQuery(
                    categoryId,
                    difficulty,
                    status),
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetRecipeList")
        .WithTags("Recipes");

        api.MapPost("/recipes", async (
            CreateRecipeRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            var recipeId = await sender.Send(
                new CreateRecipeCommand(
                    request.Title,
                    request.Slug,
                    request.Description,
                    request.CategoryId,
                    currentUser.UserId,
                    request.PrepTimeMinutes,
                    request.CookTimeMinutes,
                    request.Servings,
                    request.Difficulty),
                cancellationToken);

            return Results.Created(
                $"/api/v1/recipes/{recipeId}",
                new { id = recipeId });
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("CreateRecipe")
        .WithTags("Recipes");

        api.MapGet("/recipes/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRecipeDetailQuery(id),
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetRecipeDetail")
        .WithTags("Recipes");

        return endpoints;
    }
}

public sealed record CreateRecipeRequest(
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty);
