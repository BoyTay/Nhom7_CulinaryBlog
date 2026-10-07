using System.Globalization;
using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Recipes.Commands.ChangeRecipeLifecycle;
using CulinaryBlog.Application.Recipes.Commands.CreateRecipe;
using CulinaryBlog.Application.Recipes.Commands.UpdateRecipe;
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

        api.MapRecipeSearchEndpoints();

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
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRecipeDetailQuery(id),
                cancellationToken);

            SetRecipeEtag(httpContext, result.Version);
            return Results.Ok(result);
        })
        .WithName("GetRecipeDetail")
        .WithTags("Recipes");

        api.MapPut("/recipes/{id:guid}", async (
            Guid id,
            UpdateRecipeRequest request,
            HttpRequest httpRequest,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var precondition = GetVersionPrecondition(httpRequest, out var version);
            if (precondition is not null)
            {
                return precondition;
            }

            await sender.Send(
                new UpdateRecipeCommand(
                    id,
                    request.Title,
                    request.Slug,
                    request.Description,
                    request.CategoryId,
                    request.PrepTimeMinutes,
                    request.CookTimeMinutes,
                    request.Servings,
                    request.Difficulty,
                    request.Calories,
                    request.ProteinGrams,
                    request.CarbohydratesGrams,
                    request.FatGrams,
                    version),
                cancellationToken);

            var result = await sender.Send(new GetRecipeDetailQuery(id), cancellationToken);
            SetRecipeEtag(httpContext, result.Version);
            return Results.Ok(result);
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("UpdateRecipe")
        .WithTags("Recipes");

        MapRecipeLifecycleEndpoint(api, "/recipes/{id:guid}/publish", "PublishRecipe", RecipeLifecycleAction.Publish);
        MapRecipeLifecycleEndpoint(api, "/recipes/{id:guid}/unpublish", "UnpublishRecipe", RecipeLifecycleAction.Unpublish);
        MapRecipeLifecycleEndpoint(api, "/recipes/{id:guid}/archive", "ArchiveRecipe", RecipeLifecycleAction.Archive);

        api.MapDelete("/recipes/{id:guid}", async (
            Guid id,
            HttpRequest httpRequest,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var precondition = GetVersionPrecondition(httpRequest, out var version);
            if (precondition is not null)
            {
                return precondition;
            }

            await sender.Send(
                new ChangeRecipeLifecycleCommand(id, RecipeLifecycleAction.Delete, version),
                cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("DeleteRecipe")
        .WithTags("Recipes");

        api.MapCategoryEndpoints();
        api.MapAuthEndpoints();

        return endpoints;
    }

    private static void MapRecipeLifecycleEndpoint(
        RouteGroupBuilder api,
        string route,
        string name,
        RecipeLifecycleAction action)
    {
        api.MapPatch(route, async (
            Guid id,
            HttpRequest httpRequest,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var precondition = GetVersionPrecondition(httpRequest, out var version);
            if (precondition is not null)
            {
                return precondition;
            }

            await sender.Send(
                new ChangeRecipeLifecycleCommand(id, action, version),
                cancellationToken);

            var result = await sender.Send(new GetRecipeDetailQuery(id), cancellationToken);
            SetRecipeEtag(httpContext, result.Version);
            return Results.Ok(result);
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName(name)
        .WithTags("Recipes");
    }

    private static IResult? GetVersionPrecondition(HttpRequest request, out uint version)
    {
        version = default;
        if (!request.Headers.TryGetValue("If-Match", out var values))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "If-Match header is required",
                type: "PRECONDITION_REQUIRED");
        }

        if (values.Count != 1 || values[0] is not { } etag
            || etag.Length < 3
            || etag[0] != '"'
            || etag[^1] != '"'
            || !uint.TryParse(
                etag.AsSpan(1, etag.Length - 2),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out version))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "If-Match header must contain a recipe ETag",
                type: "INVALID_ETAG");
        }

        return null;
    }

    private static void SetRecipeEtag(HttpContext httpContext, uint version) =>
        httpContext.Response.Headers["ETag"] = $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";
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

public sealed record UpdateRecipeRequest(
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    int? Calories,
    decimal? ProteinGrams,
    decimal? CarbohydratesGrams,
    decimal? FatGrams);
