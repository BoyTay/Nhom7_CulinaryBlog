using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Security;

public static class AuthRateLimit
{
    public const int DefaultAuthPermitLimit = 10;

    /// <summary>Reads RateLimiting:AuthPermitLimit; anything that is not a positive integer keeps the default.</summary>
    public static int ResolvePermitLimit(string? configured) =>
        int.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out var limit) && limit > 0
            ? limit
            : DefaultAuthPermitLimit;

    public static void Configure(RateLimiterOptions options, int authPermitLimit = DefaultAuthPermitLimit)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }

            await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests").ExecuteAsync(context.HttpContext);
        };
        options.AddPolicy("auth-rate-limit", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                // Forwarded headers are trusted only after platform proxy middleware validates them.
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));
    }
}
