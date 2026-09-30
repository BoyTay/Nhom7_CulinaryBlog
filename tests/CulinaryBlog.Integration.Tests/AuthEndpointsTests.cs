namespace CulinaryBlog.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed class AuthEndpointsTests : IClassFixture<AuthEndpointsTests.AuthEndpointsFactory>
{
    private static int s_clientCounter;
    private readonly AuthEndpointsFactory _factory;

    public AuthEndpointsTests(AuthEndpointsFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"198.51.100.{Interlocked.Increment(ref s_clientCounter)}");
        return client;
    }

    [Fact]
    public async Task RegisterWithValidDataReturnsCreatedAndSetsSecureRefreshCookie()
    {
        var client = CreateTestClient();
        var request = new
        {
            displayName = "Gordon Ramsay",
            email = "gordon@culinary.test",
            password = "P@ssword123!",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/auth/me", response.Headers.Location?.ToString());

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.Equal(900, authResponse.ExpiresIn); // 15 minutes = 900 seconds
        Assert.Equal("gordon@culinary.test", authResponse.User.Email);
        Assert.Equal("Gordon Ramsay", authResponse.User.DisplayName);
        Assert.Contains(Roles.Author, authResponse.User.Roles);

        // Verify Set-Cookie header for refresh token
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        var cookie = Assert.Single(cookieHeaders);
        Assert.Contains("rt=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);

        // Verify that stored refresh token is hashed with SHA-256 (64 hex characters)
        Assert.NotEmpty(_factory.RefreshTokens.Tokens);
        var storedToken = _factory.RefreshTokens.Tokens[^1];
        Assert.NotNull(storedToken);
        Assert.Equal(64, storedToken.TokenHash.Length);
        Assert.True(storedToken.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task RegisterWithDuplicateEmailReturnsConflict()
    {
        var client = CreateTestClient();
        var request = new
        {
            displayName = "Duplicate Chef",
            email = "existing@culinary.test",
            password = "P@ssword123!",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("EMAIL_ALREADY_EXISTS", problem.Type);
    }

    [Fact]
    public async Task RegisterWithWeakPasswordReturnsUnprocessableEntity()
    {
        var client = CreateTestClient();
        var request = new
        {
            displayName = "Valid Name",
            email = "weakpw@culinary.test",
            password = "weak",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("VALIDATION_ERROR", problem.Type);
        Assert.True(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public async Task LoginWithValidCredentialsReturnsOkAndSetsSecureRefreshCookie()
    {
        var client = CreateTestClient();
        var request = new
        {
            email = "existing@culinary.test",
            password = "P@ssword123!",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.Equal(900, authResponse.ExpiresIn); // 15 minutes = 900 seconds
        Assert.Equal("existing@culinary.test", authResponse.User.Email);

        // Verify Set-Cookie header
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        var cookie = Assert.Single(cookieHeaders);
        Assert.Contains("rt=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginWithInvalidCredentialsReturnsUnauthorized()
    {
        var client = CreateTestClient();
        var request = new
        {
            email = "existing@culinary.test",
            password = "WrongPassword123!",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("INVALID_CREDENTIALS", problem.Type);
    }

    [Fact]
    public async Task LoginWhenAccountLockedOutReturnsUnauthorizedWithLockoutCode()
    {
        var client = CreateTestClient();
        var request = new
        {
            email = "locked@culinary.test",
            password = "P@ssword123!",
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("ACCOUNT_LOCKED", problem.Type);
    }

    [Fact]
    public async Task RateLimiterEnforcesLimitWhenRequestsExceedThreshold()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");

        var body = new { email = "rate@culinary.test", password = "P@ssword123!" };

        HttpResponseMessage lastResponse = null!;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), body);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserWhenUnauthenticatedReturnsUnauthorized()
    {
        var client = CreateTestClient();
        var response = await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfileWhenUnauthenticatedReturnsUnauthorized()
    {
        var client = CreateTestClient();
        var response = await client.PatchAsJsonAsync(new Uri("/api/v1/auth/me", UriKind.Relative), new
        {
            displayName = "New Name",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWhenUnauthenticatedReturnsUnauthorized()
    {
        var client = CreateTestClient();
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), new
        {
            refreshToken = "any-token",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class AuthEndpointsFactory : WebApplicationFactory<Program>
    {
        public TestRefreshTokenRepository RefreshTokens { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IIdentityService, TestIdentityService>();
                services.AddSingleton<IRefreshTokenRepository>(RefreshTokens);
            });
        }
    }

    private sealed class TestIdentityService : IIdentityService
    {
        private readonly Dictionary<string, (string DisplayName, string Password, bool IsLockedOut)> _users =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["existing@culinary.test"] = ("Existing Chef", "P@ssword123!", false),
                ["locked@culinary.test"] = ("Locked Chef", "P@ssword123!", true),
            };

        public Task<AuthUserDto> RegisterAsync(
            string displayName,
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (_users.ContainsKey(email))
            {
                throw new ConflictException("Email này đã được sử dụng.", "EMAIL_ALREADY_EXISTS");
            }

            _users[email] = (displayName, password, false);

            return Task.FromResult(new AuthUserDto(
                Guid.NewGuid().ToString(),
                email,
                displayName,
                null,
                null,
                [Roles.Author]));
        }

        public Task<AuthUserDto?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (!_users.TryGetValue(email, out var user))
            {
                return Task.FromResult<AuthUserDto?>(null);
            }

            if (user.IsLockedOut || user.Password != password)
            {
                return Task.FromResult<AuthUserDto?>(null);
            }

            return Task.FromResult<AuthUserDto?>(new AuthUserDto(
                "user-1",
                email,
                user.DisplayName,
                null,
                null,
                [Roles.Author]));
        }

        public Task<bool> IsLockedOutAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            if (_users.TryGetValue(email, out var user))
            {
                return Task.FromResult(user.IsLockedOut);
            }

            return Task.FromResult(false);
        }

        public Task<AuthUserDto?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthUserDto?>(new AuthUserDto(
                userId,
                "existing@culinary.test",
                "Existing Chef",
                null,
                null,
                [Roles.Author]));
        }

        public Task<AuthUserDto> FindOrCreateGoogleUserAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthUserDto(
                "google-user-1",
                "google@culinary.test",
                "Google User",
                null,
                null,
                [Roles.Author]));
        }

        public Task<AuthUserDto> UpdateProfileAsync(
            string userId,
            string? displayName,
            string? avatarUrl,
            string? bio,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthUserDto(
                userId,
                "existing@culinary.test",
                displayName ?? "Existing Chef",
                avatarUrl,
                bio,
                [Roles.Author]));
        }
    }

    public sealed class TestRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = [];

        public IReadOnlyList<RefreshToken> Tokens => _tokens;

        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            _tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            var token = _tokens.FirstOrDefault(t => t.TokenHash == tokenHash && !t.IsRevoked);
            return Task.FromResult(token);
        }

        public Task RevokeAllForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            foreach (var token in _tokens.Where(t => t.UserId == userId))
            {
                token.Revoke(revokedAt);
            }

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
