using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CulinaryBlog.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CulinaryBlog.API.Security;

/// <summary>
/// An access token stays cryptographically valid for 15 minutes, so a lock would otherwise only take effect at the
/// next refresh. Every request that carries a token therefore confirms the account is not locked by an admin.
/// </summary>
/// <remarks>
/// A locked token is refused even on endpoints guests may call. Treating it as a guest there would hide the lock:
/// reading your own draft would answer 404 and the client could not tell why. After this answer the client drops the
/// token, so public pages keep working for the same visitor.
///
/// The brute-force lockout is deliberately not checked: anyone can trigger it by typing wrong passwords for someone
/// else's email, and that must not end the owner's running session.
/// </remarks>
public static class AccountLockGuard
{
    private const string AccountLockedMarker = "CulinaryBlog.AccountLocked";

    public static JwtBearerEvents CreateJwtEvents() => new() { OnTokenValidated = MarkLockedAccountAsync };

    /// <summary>Runs right after authentication, before any endpoint or authorization decision.</summary>
    public static async Task RejectLockedAccountAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Items.ContainsKey(AccountLockedMarker))
        {
            await next(context);
            return;
        }

        // Same code as login and refresh, so clients handle a locked account in one place.
        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            type: "ACCOUNT_LOCKED",
            detail: "Tài khoản đang bị khóa.").ExecuteAsync(context);
    }

    private static async Task MarkLockedAccountAsync(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is null)
        {
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserAdministrationService>();
        if (await users.IsLockedAsync(userId, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Items[AccountLockedMarker] = true;
            context.Fail("The account is locked.");
        }
    }
}
