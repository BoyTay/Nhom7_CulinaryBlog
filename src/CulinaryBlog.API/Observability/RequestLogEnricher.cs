using System.Security.Claims;
using Serilog;

namespace CulinaryBlog.API.Observability;

public static class RequestLogEnricher
{
    // FR-OBS-002: every request log carries CorrelationId and, once authenticated, UserId.
    // Only the opaque user id is recorded; never the token, e-mail address or other claims.
    public static void Enrich(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    }
}
