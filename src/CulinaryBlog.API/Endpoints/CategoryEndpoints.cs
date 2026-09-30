using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Categories;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder api)
    {
        var categories = api.MapGroup("/categories").WithTags("Categories");

        categories.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetCategoriesQuery(), cancellationToken)))
            .WithName("GetCategories")
            .WithSummary("List categories with published recipe counts")
            .Produces<IReadOnlyList<CategoryDto>>()
            .AllowAnonymous();

        categories.MapGet("/{slug}", async (
            string slug, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetCategoryBySlugQuery(slug, page ?? 1, pageSize ?? 12), cancellationToken)))
            .WithName("GetCategoryBySlug")
            .WithSummary("Get a category and its visible recipes")
            .Produces<CategoryDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AllowAnonymous();

        categories.MapPost("/", async (
            CategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var created = await sender.Send(
                new CreateCategoryCommand(request.Name, request.Description, request.ImageUrl, request.OrderIndex),
                cancellationToken);
            return Results.Created($"/api/v1/categories/{created.Slug}", created);
        })
            .WithName("CreateCategory")
            .WithSummary("Create a category")
            .RequireAuthorization(Policies.RequireAdmin)
            .Produces<CategoryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        categories.MapPut("/{id:guid}", async (
            Guid id, CategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpdateCategoryCommand(id, request.Name, request.Description, request.ImageUrl, request.OrderIndex),
                cancellationToken)))
            .WithName("UpdateCategory")
            .WithSummary("Update a category while preserving its slug")
            .RequireAuthorization(Policies.RequireAdmin)
            .Produces<CategoryDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        categories.MapDelete("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
            return Results.NoContent();
        })
            .WithName("DeleteCategory")
            .WithSummary("Soft delete an empty category")
            .RequireAuthorization(Policies.RequireAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }
}

public sealed record CategoryRequest(string Name, string? Description, string? ImageUrl, int OrderIndex = 0);
