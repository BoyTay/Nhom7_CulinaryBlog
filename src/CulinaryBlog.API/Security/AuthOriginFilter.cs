using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.API.Security;

/// <summary>Enforces the configured browser origin boundary before Auth can change state.</summary>
public sealed class AuthOriginFilter(IOptions<CorsOptions> corsOptions) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) || !request.Headers.ContainsKey("Origin"))
        {
            // Non-browser clients need not send Origin. Browsers send it for unsafe requests.
            return next(context);
        }

        var origins = request.Headers.Origin;
        var policy = corsOptions.Value.GetPolicy(corsOptions.Value.DefaultPolicyName);
        if (origins.Count != 1 || origins[0] is not { } origin ||
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase) ||
            !(string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase) ||
                (policy is not null && policy.IsOriginAllowed(origin))))
        {
            return ValueTask.FromResult<object?>(Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden"));
        }

        return next(context);
    }
}
