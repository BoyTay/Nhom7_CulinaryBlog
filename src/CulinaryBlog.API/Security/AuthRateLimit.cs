using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Security;

public static class AuthRateLimit
{
    public static void Configure(RateLimiterOptions options)
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
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));
    }
}
