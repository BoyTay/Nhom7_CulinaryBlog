using CulinaryBlog.Application.Common.Pagination;

namespace CulinaryBlog.Application.Users;

public static class ManagedUserStatus
{
    public const string Active = "Active";
    public const string Locked = "Locked";
}

public sealed record ManagedUserFilter(string? Search, string? Role, string? Status, int Page, int PageSize);

public sealed record ManagedUserDto(
    string Id,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string Role,
    string Status,
    DateTimeOffset CreatedAt,
    int RecipeCount,
    DateTimeOffset? LockedUntil,
    int? LockDays);

// Counts cover every account, not just the current filter, so the stat cards and chips stay stable while searching.
public sealed record ManagedUserSummaryDto(int Total, int Authors, int Admins, int Locked, int JoinedLast30Days);

public sealed record ManagedUserListDto(ManagedUserSummaryDto Summary, PagedResult<ManagedUserDto> Users);
