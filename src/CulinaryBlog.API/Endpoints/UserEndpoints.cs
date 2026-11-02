using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Users;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization(Policies.RequireAdmin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        users.MapGet("/", async (
            string? q, string? role, string? status, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetManagedUsersQuery(q, role, status, page ?? 1, pageSize ?? 8), cancellationToken)))
            .WithName("GetManagedUsers")
            .WithSummary("List Admin and Author accounts")
            .WithDescription("Admin only. Accent-insensitive search on name and email. role: Admin or Author; status: Active or Locked. The summary counts every account regardless of the filter. Errors: VALIDATION_ERROR (422).")
            .Produces<ManagedUserListDto>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        users.MapPut("/{id}/role", async (
            string id, ChangeUserRoleRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ChangeUserRoleCommand(id, request.Role), cancellationToken);
            return Results.NoContent();
        })
            .WithName("ChangeUserRole")
            .WithSummary("Change an account's role")
            .WithDescription("Admin only; applies to the user's next token refresh. Errors: USER_SELF_MANAGEMENT_FORBIDDEN (403), ADMIN_RIGHTS_REVOKED (403), USER_NOT_FOUND (404), VALIDATION_ERROR (422).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        users.MapPut("/{id}/lock", async (
            string id, SetUserLockRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new SetUserLockCommand(id, request.Locked, request.Days), cancellationToken);
            return Results.NoContent();
        })
            .WithName("SetUserLock")
            .WithSummary("Lock or unlock an account")
            .WithDescription("Admin only. days (1-365) sets when the lock ends on its own; omit it for a lock that lasts until an admin lifts it. Locking revokes the user's refresh tokens, and a password reset does not lift it. Errors: USER_SELF_MANAGEMENT_FORBIDDEN (403), ADMIN_RIGHTS_REVOKED (403), USER_NOT_FOUND (404), VALIDATION_ERROR (422).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }
}

public sealed record ChangeUserRoleRequest(string Role);

public sealed record SetUserLockRequest(bool Locked, int? Days = null);
