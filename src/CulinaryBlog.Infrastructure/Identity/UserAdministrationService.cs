using System.Linq.Expressions;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Application.Users;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Identity;

public sealed class UserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokens,
    TimeProvider timeProvider) : IUserAdministrationService
{
    // Serializes account changes so two admins cannot remove each other's rights at the same moment.
    private const long AdministrationLockKey = 7_402_100_001;

    private IQueryable<ApplicationUser> Accounts => dbContext.Users.AsNoTracking();

    private IQueryable<string> AdminIds =>
        dbContext.UserRoles
            .Join(dbContext.Roles.Where(role => role.Name == Roles.Admin), link => link.RoleId, role => role.Id, (link, _) => link.UserId);

    public async Task<ManagedUserListDto> ListAsync(ManagedUserFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var adminIds = AdminIds;
        var now = timeProvider.GetUtcNow();
        var joinedSince = now.AddDays(-30);

        var total = await Accounts.CountAsync(cancellationToken);
        var admins = await Accounts.CountAsync(user => adminIds.Contains(user.Id), cancellationToken);
        var locked = await Accounts.CountAsync(LockedAt(now), cancellationToken);
        var recent = await Accounts.CountAsync(user => user.CreatedAt >= joinedSince, cancellationToken);

        var matching = ApplyFilter(Accounts, filter, adminIds, now);
        var totalCount = await matching.CountAsync(cancellationToken);
        var rows = await matching
            .OrderBy(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new
            {
                User = user,
                IsAdmin = adminIds.Contains(user.Id),
                RecipeCount = dbContext.Recipes.Count(recipe => recipe.AuthorId == user.Id),
            })
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(row => ToDto(row.User, row.IsAdmin, row.RecipeCount, now));

        return new ManagedUserListDto(
            new ManagedUserSummaryDto(total, total - admins, admins, locked, recent),
            PagedResult.Create(items, totalCount, filter.Page, filter.PageSize));
    }

    private static ManagedUserDto ToDto(ApplicationUser user, bool isAdmin, int recipeCount, DateTimeOffset now)
    {
        var locked = user.IsLockedAt(now);
        var lockDays = locked && user is { LockedAt: { } from, LockedUntil: { } until }
            ? (int?)Math.Round((until - from).TotalDays)
            : null;

        return new ManagedUserDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.AvatarUrl,
            isAdmin ? Roles.Admin : Roles.Author,
            locked ? ManagedUserStatus.Locked : ManagedUserStatus.Active,
            user.CreatedAt,
            recipeCount,
            locked ? user.LockedUntil : null,
            lockDays);
    }

    public Task<bool> IsLockedAsync(string userId, CancellationToken cancellationToken = default) =>
        Accounts.Where(user => user.Id == userId).AnyAsync(LockedAt(timeProvider.GetUtcNow()), cancellationToken);

    public Task ChangeRoleAsync(string actorId, string userId, string role, CancellationToken cancellationToken = default) =>
        ManageAsync(actorId, userId, async user =>
        {
            var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
            if (role == Roles.Admin && !isAdmin)
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, Roles.Admin));
            }
            else if (role == Roles.Author && isAdmin)
            {
                EnsureSucceeded(await userManager.RemoveFromRoleAsync(user, Roles.Admin));
            }

            if (!await userManager.IsInRoleAsync(user, Roles.Author))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, Roles.Author));
            }
        }, cancellationToken);

    public Task SetLockedAsync(string actorId, string userId, bool locked, int? days, CancellationToken cancellationToken = default) =>
        ManageAsync(actorId, userId, async user =>
        {
            var now = timeProvider.GetUtcNow();
            user.IsActive = !locked;
            user.LockedAt = locked ? now : null;
            user.LockedUntil = locked && days is { } period ? now.AddDays(period) : null;
            EnsureSucceeded(await userManager.UpdateAsync(user));
            if (locked)
            {
                await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);
                await refreshTokens.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

    private async Task ManageAsync(
        string actorId,
        string userId,
        Func<ApplicationUser, Task> change,
        CancellationToken cancellationToken)
    {
        if (string.Equals(actorId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                "Không thể tự thay đổi tài khoản đang đăng nhập.",
                "USER_SELF_MANAGEMENT_FORBIDDEN");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdministrationLockKey})", cancellationToken);

        // The JWT can be 15 minutes old, so rights are confirmed against the database inside the lock.
        var adminIds = AdminIds;
        var actorIsAdmin = await Accounts
            .Where(NotLockedAt(timeProvider.GetUtcNow()))
            .AnyAsync(user => user.Id == actorId && adminIds.Contains(user.Id), cancellationToken);
        if (!actorIsAdmin)
        {
            throw new ForbiddenException("Tài khoản của bạn không còn quyền quản trị.", "ADMIN_RIGHTS_REVOKED");
        }

        var target = await dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.", "USER_NOT_FOUND");

        await change(target);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IQueryable<ApplicationUser> ApplyFilter(
        IQueryable<ApplicationUser> accounts,
        ManagedUserFilter filter,
        IQueryable<string> adminIds,
        DateTimeOffset now)
    {
        accounts = filter.Role switch
        {
            Roles.Admin => accounts.Where(user => adminIds.Contains(user.Id)),
            Roles.Author => accounts.Where(user => !adminIds.Contains(user.Id)),
            _ => accounts,
        };

        accounts = filter.Status switch
        {
            ManagedUserStatus.Active => accounts.Where(NotLockedAt(now)),
            ManagedUserStatus.Locked => accounts.Where(LockedAt(now)),
            _ => accounts,
        };

        if (string.IsNullOrWhiteSpace(filter.Search))
        {
            return accounts;
        }

        // Accent-insensitive, as the design searches: "hoai" finds "Hoài".
        var pattern = $"%{EscapeLikePattern(filter.Search)}%";
        return accounts.Where(user => EF.Functions.ILike(
            EF.Functions.Unaccent(user.DisplayName + " " + (user.Email ?? string.Empty)),
            EF.Functions.Unaccent(pattern),
            "\\"));
    }

    // The query form of ApplicationUser.IsLockedAt: a lock past its end date no longer counts.
    private static Expression<Func<ApplicationUser, bool>> LockedAt(DateTimeOffset now) =>
        user => !user.IsActive && (user.LockedUntil == null || user.LockedUntil > now);

    private static Expression<Func<ApplicationUser, bool>> NotLockedAt(DateTimeOffset now) =>
        user => user.IsActive || (user.LockedUntil != null && user.LockedUntil <= now);

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Code));
            throw new ConflictException($"Không thể cập nhật tài khoản: {errors}", "USER_UPDATE_FAILED");
        }
    }
}
