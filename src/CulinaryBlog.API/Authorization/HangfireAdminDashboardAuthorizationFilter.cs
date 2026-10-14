using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Hangfire.Dashboard;

namespace CulinaryBlog.API.Authorization;

public static class HangfireDashboardAccess
{
    public static bool IsAdmin(
        ClaimsPrincipal user,
        IAuthorizationService authorizationService)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(authorizationService);

        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return authorizationService
            .AuthorizeAsync(user, resource: null, policyName: Policies.AdminPolicy)
            .GetAwaiter()
            .GetResult()
            .Succeeded;
    }
}

public sealed class HangfireAdminDashboardAuthorizationFilter(
    IAuthorizationService authorizationService) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return HangfireDashboardAccess.IsAdmin(
            context.GetHttpContext().User,
            authorizationService);
    }
}
