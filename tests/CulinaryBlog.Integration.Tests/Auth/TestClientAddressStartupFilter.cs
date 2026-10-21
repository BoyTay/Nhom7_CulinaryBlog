using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CulinaryBlog.Integration.Tests.Auth;

// TestServer has no socket peer. Set it explicitly; production never trusts this header.
internal sealed class TestClientAddressStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(
                context.Request.Headers["X-Test-Client-IP"].FirstOrDefault() ?? "192.0.2.1");
            await nextMiddleware(context);
        });
        next(app);
    };
}
