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

        return endpoints;
    }
}
