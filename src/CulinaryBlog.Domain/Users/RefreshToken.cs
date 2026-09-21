namespace CulinaryBlog.Domain.Users;

using CulinaryBlog.Domain.Common;

public sealed class RefreshToken : Entity
{
    private RefreshToken() : base(Guid.NewGuid())
    {
    }

    private RefreshToken(
        Guid id,
        string userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? createdByIp) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp;
    }

    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(
        string userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? createdByIp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            createdAt,
            createdByIp);
    }

    public void Revoke(DateTimeOffset revokedAt, string? replacedByTokenHash = null)
    {
        RevokedAt = revokedAt;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
