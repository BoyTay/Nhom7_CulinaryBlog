using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.API.Observability;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // Names each failing dependency so operators do not need log access to triage a 503.
    // Exceptions and connection details are deliberately left out of the body.
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }
}
