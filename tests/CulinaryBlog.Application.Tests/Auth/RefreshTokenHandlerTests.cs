using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Commands.Logout;
using CulinaryBlog.Application.Auth.Commands.RefreshToken;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Users;

namespace CulinaryBlog.Application.Tests.Auth;

public sealed class RefreshTokenHandlerTests
{
    [Fact]
    public async Task RotationRevokesPredecessorAndStoresOnlyHash()
    {
        var repository = new TokenRepository();
        var old = repository.Add("user", "hash:old");
        var handler = new RefreshTokenCommandHandler(new IdentityStub(), new JwtStub(), repository);

        var result = await handler.Handle(new RefreshTokenCommand("old", "192.0.2.1"), default);

        Assert.True(old.IsRevoked);
        var replacement = Assert.Single(repository.Tokens, t => t.IsActive);
        Assert.Equal("hash:new", replacement.TokenHash);
        Assert.Equal(replacement.TokenHash, old.ReplacedByTokenHash);
        Assert.Equal("192.0.2.1", replacement.CreatedByIp);
        Assert.Equal("new", result.RefreshToken);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ReuseRevokesEverySessionOfOwnerAndPreservesOtherUsers()
    {
        var repository = new TokenRepository();
        repository.Add("user", "hash:old").Revoke(DateTimeOffset.UtcNow);
        repository.Add("user", "hash:session-two");
        var other = repository.Add("other", "hash:other");
        var handler = new RefreshTokenCommandHandler(new IdentityStub(), new JwtStub(), repository);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new RefreshTokenCommand("old"), default));

        Assert.Equal("REVOKED_REFRESH_TOKEN", error.Code);
        Assert.All(repository.Tokens.Where(t => t.UserId == "user"), t => Assert.True(t.IsRevoked));
        Assert.True(other.IsActive);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData("unknown", "INVALID_REFRESH_TOKEN")]
    [InlineData("expired", "EXPIRED_REFRESH_TOKEN")]
    [InlineData("missing-user", "USER_NOT_FOUND")]
    public async Task InvalidRefreshDoesNotIssueOrPersistTokens(string scenario, string code)
    {
        var repository = new TokenRepository();
        if (scenario != "unknown")
        {
            repository.Tokens.Add(RefreshToken.Create("user", "hash:old",
                DateTimeOffset.UtcNow.AddDays(scenario == "expired" ? -1 : 1), DateTimeOffset.UtcNow));
        }

        var count = repository.Tokens.Count;
        var handler = new RefreshTokenCommandHandler(new IdentityStub { UserMissing = scenario == "missing-user" },
            new JwtStub(), repository);
        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new RefreshTokenCommand("old"), default));

        Assert.Equal(code, error.Code);
        Assert.Equal(count, repository.Tokens.Count);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task LockedAccountCannotRefreshAndAllItsSessionsAreRevoked()
    {
        var repository = new TokenRepository();
        repository.Add("user", "hash:old");
        repository.Add("user", "hash:second-session");
        var handler = new RefreshTokenCommandHandler(new IdentityStub { Locked = true }, new JwtStub(), repository);

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new RefreshTokenCommand("old"), default));

        Assert.Equal("ACCOUNT_LOCKED", error.Code);
        Assert.Equal(2, repository.Tokens.Count);
        Assert.All(repository.Tokens, t => Assert.True(t.IsRevoked));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task LogoutIsIdempotentAndRevokesOnlySuppliedSession()
    {
        var repository = new TokenRepository();
        var token = repository.Add("user", "hash:old");
        var other = repository.Add("user", "hash:other");
        var handler = new LogoutCommandHandler(new JwtStub(), repository);

        await handler.Handle(new LogoutCommand("old"), default);
        await handler.Handle(new LogoutCommand("old"), default);
        await handler.Handle(new LogoutCommand(null), default);
        await handler.Handle(new LogoutCommand("unknown"), default);

        Assert.True(token.IsRevoked);
        Assert.True(other.IsActive);
        Assert.Equal(1, repository.SaveCount);
    }

    private sealed class TokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Tokens { get; } = [];
        public int SaveCount { get; private set; }
        public RefreshToken Add(string userId, string hash)
        {
            var token = RefreshToken.Create(userId, hash, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow);
            Tokens.Add(token);
            return token;
        }

        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            Tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.Find(t => t.TokenHash == tokenHash));

        public Task RevokeAllForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            foreach (var token in Tokens.Where(t => t.UserId == userId && !t.IsRevoked)) token.Revoke(revokedAt);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class JwtStub : IJwtService
    {
        public string GenerateAccessToken(string userId, string email, string displayName, IReadOnlyList<string> roles) => "access";
        public string GenerateRefreshToken() => "new";
        public string HashToken(string token) => $"hash:{token}";
    }

    private sealed class IdentityStub : IIdentityService
    {
        public bool UserMissing { get; init; }
        public bool Locked { get; init; }
        public Task<AuthUserDto?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthUserDto?>(UserMissing ? null : new(userId, "chef@example.test", "Chef", null, null, ["Author"]));
        public Task<bool> IsLockedOutAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(Locked);
        public Task<AuthUserDto> RegisterAsync(string displayName, string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto> FindOrCreateGoogleUserAsync(string idToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto> UpdateProfileAsync(string userId, string? displayName, string? avatarUrl, string? bio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
