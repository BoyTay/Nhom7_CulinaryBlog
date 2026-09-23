using CulinaryBlog.Application.Recipes.Queries.GetRecipeList;
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

        return endpoints;
    }
}