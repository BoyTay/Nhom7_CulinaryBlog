using CulinaryBlog.Application.Users;

namespace CulinaryBlog.Application.Abstractions.Identity;

public interface IUserAdministrationService
{
    Task<ManagedUserListDto> ListAsync(ManagedUserFilter filter, CancellationToken cancellationToken = default);

    // Runs on every signed-in request, so it must stay a single primary-key lookup.
    Task<bool> IsLockedAsync(string userId, CancellationToken cancellationToken = default);

    Task ChangeRoleAsync(string actorId, string userId, string role, CancellationToken cancellationToken = default);

    // Days is null for a lock that lasts until an admin lifts it.
    Task SetLockedAsync(string actorId, string userId, bool locked, int? days, CancellationToken cancellationToken = default);
}
