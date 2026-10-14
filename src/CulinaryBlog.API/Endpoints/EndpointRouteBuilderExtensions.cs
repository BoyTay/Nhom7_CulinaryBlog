using System.Globalization;
using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Recipes.Commands.ChangeRecipeLifecycle;
using CulinaryBlog.Application.Recipes.Commands.CreateRecipe;
using CulinaryBlog.Application.Recipes.Commands.ManageRecipeIngredient;
using CulinaryBlog.Application.Recipes.Commands.ManageRecipeStep;
using CulinaryBlog.Application.Recipes.Commands.UpdateRecipe;
using CulinaryBlog.Application.Recipes.Images;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
using CulinaryBlog.Domain.Recipes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

            var result = await sender.Send(
                new GetRecipeDetailQuery(id),
                cancellationToken);

            SetRecipeEtag(httpContext, result.Version);
            return Results.Ok(result);
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("UpdateRecipe")
        .WithTags("Recipes");

        MapRecipeLifecycleEndpoint(
            api,
            "/recipes/{id:guid}/publish",
            "PublishRecipe",
            RecipeLifecycleAction.Publish);

        MapRecipeLifecycleEndpoint(
            api,
            "/recipes/{id:guid}/unpublish",
            "UnpublishRecipe",
            RecipeLifecycleAction.Unpublish);

        MapRecipeLifecycleEndpoint(
            api,
            "/recipes/{id:guid}/archive",
            "ArchiveRecipe",
            RecipeLifecycleAction.Archive);

        MapRecipeStepEndpoints(api);
        MapRecipeIngredientEndpoints(api);
        MapRecipeImageEndpoints(api);

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
                new ChangeRecipeLifecycleCommand(
                    id,
                    RecipeLifecycleAction.Delete,
                    version),
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

            var result = await sender.Send(
                new GetRecipeDetailQuery(id),
                cancellationToken);

            SetRecipeEtag(httpContext, result.Version);
            return Results.Ok(result);
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName(name)
        .WithTags("Recipes");
    }

    private static void MapRecipeStepEndpoints(RouteGroupBuilder api)
    {
        // Create recipe step
        api.MapPost("/recipes/{recipeId:guid}/steps", async (
            Guid recipeId,
            RecipeStepRequest request,
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

            var stepId = await sender.Send(
                new ManageRecipeStepCommand(
                    recipeId,
                    null,
                    RecipeChildAction.Create,
                    request.Description,
                    request.TimerMinutes,
                    request.ImageUrl,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.Created(
                $"/api/v1/recipes/{recipeId}/steps/{stepId}",
                new { id = stepId });
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("CreateRecipeStep")
        .WithTags("Recipes")
        .WithSummary("Add a step to a recipe")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        // Update recipe step
        api.MapPut("/recipes/{recipeId:guid}/steps/{stepId:guid}", async (
            Guid recipeId,
            Guid stepId,
            RecipeStepRequest request,
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
                new ManageRecipeStepCommand(
                    recipeId,
                    stepId,
                    RecipeChildAction.Update,
                    request.Description,
                    request.TimerMinutes,
                    request.ImageUrl,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.Ok(
                detail.Steps.Single(step => step.Id == stepId));
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("UpdateRecipeStep")
        .WithTags("Recipes")
        .WithSummary("Update a recipe step")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        // Delete recipe step
        api.MapDelete("/recipes/{recipeId:guid}/steps/{stepId:guid}", async (
            Guid recipeId,
            Guid stepId,
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
                new ManageRecipeStepCommand(
                    recipeId,
                    stepId,
                    RecipeChildAction.Delete,
                    null,
                    null,
                    null,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.NoContent();
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("DeleteRecipeStep")
        .WithTags("Recipes")
        .WithSummary("Delete a recipe step")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);
    }

    private static void MapRecipeIngredientEndpoints(RouteGroupBuilder api)
    {
        // Create recipe ingredient
        api.MapPost("/recipes/{recipeId:guid}/ingredients", async (
            Guid recipeId,
            RecipeIngredientRequest request,
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

            var ingredientId = await sender.Send(
                new ManageRecipeIngredientCommand(
                    recipeId,
                    null,
                    RecipeChildAction.Create,
                    request.Name,
                    request.Quantity,
                    request.Unit,
                    request.Notes,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.Created(
                $"/api/v1/recipes/{recipeId}/ingredients/{ingredientId}",
                new { id = ingredientId });
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("CreateRecipeIngredient")
        .WithTags("Recipes")
        .WithSummary("Add an ingredient to a recipe")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        // Update recipe ingredient
        api.MapPut("/recipes/{recipeId:guid}/ingredients/{ingredientId:guid}", async (
            Guid recipeId,
            Guid ingredientId,
            RecipeIngredientRequest request,
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
                new ManageRecipeIngredientCommand(
                    recipeId,
                    ingredientId,
                    RecipeChildAction.Update,
                    request.Name,
                    request.Quantity,
                    request.Unit,
                    request.Notes,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.Ok(
                detail.Ingredients.Single(
                    ingredient => ingredient.Id == ingredientId));
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("UpdateRecipeIngredient")
        .WithTags("Recipes")
        .WithSummary("Update a recipe ingredient")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        // Delete recipe ingredient
        api.MapDelete("/recipes/{recipeId:guid}/ingredients/{ingredientId:guid}", async (
            Guid recipeId,
            Guid ingredientId,
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
                new ManageRecipeIngredientCommand(
                    recipeId,
                    ingredientId,
                    RecipeChildAction.Delete,
                    null,
                    null,
                    null,
                    null,
                    version),
                cancellationToken);

            var detail = await sender.Send(
                new GetRecipeDetailQuery(recipeId),
                cancellationToken);

            SetRecipeEtag(httpContext, detail.Version);

            return Results.NoContent();
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("DeleteRecipeIngredient")
        .WithTags("Recipes")
        .WithSummary("Delete a recipe ingredient")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);
    }

    private static void MapRecipeImageEndpoints(RouteGroupBuilder api)
    {
        api.MapPost("/recipes/{recipeId:guid}/images", async (
            Guid recipeId,
            IFormFile? file,
            [FromForm(Name = "altText")] string? altText,
            [FromForm(Name = "isPrimary")] bool? isPrimary,
            HttpRequest httpRequest,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (file is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "An image file is required.",
                    type: "FILE_REQUIRED");
            }

            var precondition = GetVersionPrecondition(httpRequest, out var version);
            if (precondition is not null)
            {
                return precondition;
            }

            await using var content = file.OpenReadStream();
            var imageId = await sender.Send(
                new UploadRecipeImageCommand(
                    recipeId,
                    content,
                    file.Length,
                    file.ContentType,
                    altText,
                    isPrimary ?? false,
                    version),
                cancellationToken);
            var detail = await sender.Send(new GetRecipeDetailQuery(recipeId), cancellationToken);
            SetRecipeEtag(httpContext, detail.Version);
            var image = detail.Images.Single(item => item.Id == imageId);

            return Results.Created($"/api/v1/recipes/{recipeId}/images/{imageId}", image);
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(6 * 1024 * 1024))
        .WithMetadata(new RequestFormLimitsAttribute
        {
            MultipartBodyLengthLimit = RecipeImageFileValidator.MaximumFileSizeBytes + 64 * 1024,
        })
        .WithName("UploadRecipeImage")
        .WithTags("Recipes")
        .WithSummary("Upload an image for a recipe")
        .WithDescription("Owner/Admin only. Send multipart field file (JPEG, PNG, WebP or AVIF, at most 5 MiB) and the current recipe ETag in If-Match. Returns the new ETag. The first image is primary automatically; optional isPrimary selects a replacement. Larger HTTP requests may be rejected with 413 before file validation.")
        .Produces<RecipeImageDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapPatch("/recipes/{recipeId:guid}/images/{imageId:guid}/primary", async (
            Guid recipeId,
            Guid imageId,
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
                new ManageRecipeImageCommand(recipeId, imageId, RecipeImageAction.SetPrimary, version),
                cancellationToken);
            var detail = await sender.Send(new GetRecipeDetailQuery(recipeId), cancellationToken);
            SetRecipeEtag(httpContext, detail.Version);
            return Results.Ok(detail.Images.Single(image => image.Id == imageId));
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("SetRecipePrimaryImage")
        .WithTags("Recipes")
        .WithSummary("Set the primary image for a recipe")
        .WithDescription("Owner/Admin only. Requires the current recipe ETag in If-Match; returns the updated ETag. A stale version returns 422 RECIPE_CONCURRENCY_CONFLICT.")
        .Produces<RecipeImageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        api.MapDelete("/recipes/{recipeId:guid}/images/{imageId:guid}", async (
            Guid recipeId,
            Guid imageId,
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
                new ManageRecipeImageCommand(recipeId, imageId, RecipeImageAction.Delete, version),
                cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(Policies.RequireAuthor)
        .WithName("DeleteRecipeImage")
        .WithTags("Recipes")
        .WithSummary("Delete an image from a recipe")
        .WithDescription("Owner/Admin only. Requires the current recipe ETag in If-Match. Deleting the primary image selects the first remaining image; deleting the last image leaves the recipe without images.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static IResult? GetVersionPrecondition(
        HttpRequest request,
        out uint version)
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

    private static void SetRecipeEtag(
        HttpContext httpContext,
        uint version) =>
        httpContext.Response.Headers["ETag"] =
            $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";
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

public sealed record RecipeStepRequest(
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record RecipeIngredientRequest(
    string Name,
    decimal? Quantity,
    string Unit,
    string? Notes);
