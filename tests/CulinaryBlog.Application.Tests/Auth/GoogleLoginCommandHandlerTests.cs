using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Commands.GoogleLogin;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Auth.Events;
using CulinaryBlog.Domain.Users;
using MediatR;

namespace CulinaryBlog.Application.Tests.Auth;

public sealed class GoogleLoginCommandHandlerTests
{
    [Fact]
    public async Task NewGoogleAccountRaisesUserRegisteredSoWelcomeEmailIsSent()
    {
        var publisher = new RecordingPublisher();
        var handler = CreateHandler(isNewUser: true, publisher);

        var result = await handler.Handle(new GoogleLoginCommand("id-token", "192.0.2.1"), default);

        var registered = Assert.IsType<UserRegisteredEvent>(Assert.Single(publisher.Published));
        Assert.Equal("google-user", registered.UserId);
        Assert.Equal("cook@example.test", registered.Email);
        Assert.Equal("Google Cook", registered.DisplayName);
        Assert.Equal("access", result.AccessToken);
    }

    [Fact]
    public async Task ReturningGoogleAccountDoesNotRaiseUserRegisteredAgain()
    {
        var publisher = new RecordingPublisher();
        var handler = CreateHandler(isNewUser: false, publisher);

        await handler.Handle(new GoogleLoginCommand("id-token", "192.0.2.1"), default);

        Assert.Empty(publisher.Published);
    }

    private static GoogleLoginCommandHandler CreateHandler(bool isNewUser, RecordingPublisher publisher) =>
        new(new GoogleIdentityStub(isNewUser), new JwtStub(), new TokenRepositoryStub(), publisher);

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Published { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class JwtStub : IJwtService
    {
        public string GenerateAccessToken(string userId, string email, string displayName, IReadOnlyList<string> roles) => "access";
        public string GenerateRefreshToken() => "refresh";
        public string HashToken(string token) => $"hash:{token}";
    }

    private sealed class TokenRepositoryStub : IRefreshTokenRepository
    {
        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<RefreshToken?>(null);
        public Task RevokeAllForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class GoogleIdentityStub(bool isNewUser) : IIdentityService
    {
        public Task<GoogleSignInResult> FindOrCreateGoogleUserAsync(string idToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GoogleSignInResult(
                new AuthUserDto("google-user", "cook@example.test", "Google Cook", null, null, ["Author"]),
                isNewUser));

        public Task<AuthUserDto> RegisterAsync(string displayName, string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto> UpdateProfileAsync(string userId, string? displayName, string? avatarUrl, string? bio, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsLockedOutAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
