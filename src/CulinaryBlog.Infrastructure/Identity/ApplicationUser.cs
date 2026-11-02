namespace CulinaryBlog.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // An admin's lock. IsActive stays false after LockedUntil passes; the lock simply no longer applies.
    // No LockedUntil means the lock lasts until an admin lifts it.
    public DateTimeOffset? LockedAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }

    public bool IsLockedAt(DateTimeOffset now) => !IsActive && (LockedUntil is null || LockedUntil > now);
}
