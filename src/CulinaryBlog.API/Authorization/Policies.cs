namespace CulinaryBlog.API.Authorization;

using CulinaryBlog.Application.Auth.Common;
using Microsoft.AspNetCore.Authorization;

public static class Policies
{
    public const string RequireAuthor = "RequireAuthor";
    public const string RequireAdmin = "RequireAdmin";

    public static void ConfigureAuthorization(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(RequireAuthor, policy =>
            policy.RequireRole(Roles.Author, Roles.Admin));

        options.AddPolicy(RequireAdmin, policy =>
            policy.RequireRole(Roles.Admin));
    }
}
