using CulinaryBlog.Domain.Users;

namespace CulinaryBlog.Application.Tests.Auth;

public sealed class RefreshTokenTests
{
    [Fact]
    public void CreateWithValidParametersSetsPropertiesAndIsActive()
    {
        var userId = Guid.NewGuid().ToString();
        var tokenHash = "abc123hash";
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(7);
        var ip = "127.0.0.1";

        var token = RefreshToken.Create(userId, tokenHash, expiresAt, createdAt, ip);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(tokenHash, token.TokenHash);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal(createdAt, token.CreatedAt);
        Assert.Equal(ip, token.CreatedByIp);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenHash);
        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("   ", "hash")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    [InlineData("user", "   ")]
    public void CreateWithInvalidArgumentsThrowsArgumentException(string? userId, string? tokenHash)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.ThrowsAny<ArgumentException>(() =>
            RefreshToken.Create(userId!, tokenHash!, now.AddDays(7), now));
    }

    [Fact]
    public void IsExpiredWhenExpirationInPastReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var token = RefreshToken.Create(
            "user-1",
            "hash-1",
            now.AddMinutes(-5),
            now.AddDays(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void RevokeSetsRevocationDetailsAndDeactivatesToken()
    {
        var now = DateTimeOffset.UtcNow;
        var token = RefreshToken.Create("user-1", "hash-1", now.AddDays(7), now);
        var revokedAt = now.AddHours(1);
        var replacementHash = "hash-replacement";

        token.Revoke(revokedAt, replacementHash);

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.Equal(replacementHash, token.ReplacedByTokenHash);
    }
}
