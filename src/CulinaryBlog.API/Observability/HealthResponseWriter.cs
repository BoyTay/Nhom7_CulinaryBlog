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
        // Shape from SRS 8.7: { "status": "...", "entries": { "<check name>": { "status": "..." } } }.
        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }
}
