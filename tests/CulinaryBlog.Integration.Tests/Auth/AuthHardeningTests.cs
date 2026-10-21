using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Integration.Tests.Auth;

public sealed class AuthHardeningTests(AuthDatabaseFactory factory) : IClassFixture<AuthDatabaseFactory>
{
    private static int s_clientId;

    [Fact]
    public async Task FailedAdminSeedLogsOnlyErrorCodesWithoutPersonalData()
    {
        using var scope = factory.Services.CreateScope();
        var email = $"private seed {Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Email"] = email,
            ["Admin:Password"] = password,
        }).Build();
        var seeder = new RoleSeeder(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), configuration,
            scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>());

        await seeder.SeedAsync();

        var failures = factory.Logs.Where(entry => entry.MessageTemplate.Text.Contains("Failed to seed admin", StringComparison.Ordinal));
        Assert.NotEmpty(failures);
        var text = string.Join('\n', failures.Select(RenderLog));
        Assert.Contains("InvalidUserName", text, StringComparison.Ordinal);
        Assert.DoesNotContain(email, text, StringComparison.Ordinal);
        Assert.DoesNotContain(password, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredRefreshDoesNotRotateAndReleasesItsDatabaseLock()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokens
                .Where(t => t.UserId == session.Auth.User.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddDays(-1)));
        }

        using var expired = await RefreshAsync(client, session.Refresh);
        await AssertProblemAsync(expired, HttpStatusCode.Unauthorized, "EXPIRED_REFRESH_TOKEN");
        Assert.False(expired.Headers.Contains("Set-Cookie"));
        using var login = await LoginAsync(client, session.Auth.User.Email, session.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var fresh = await RefreshAsync(client, CookieToken(login));
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        Assert.Equal(3, await verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokens
            .CountAsync(t => t.UserId == session.Auth.User.Id));
    }

    [Fact]
    public async Task ConcurrentRefreshOfSameTokenIssuesOnlyOneSuccessorAndDetectsReuse()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => RefreshAsync(client, session.Refresh)));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var reuse = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            await AssertProblemAsync(reuse, HttpStatusCode.Unauthorized, "REVOKED_REFRESH_TOKEN");
            using var scope = factory.Services.CreateScope();
            var tokens = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokens
                .Where(t => t.UserId == session.Auth.User.Id).ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.All(tokens, token => Assert.True(token.IsRevoked));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task SameOriginBrowserCanRotateItsCookie()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        client.DefaultRequestHeaders.Add("Origin", "https://localhost");
        using var response = await RefreshAsync(client, session.Refresh);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertCookieFlags(response);
    }

    [Fact]
    public async Task RealIdentityLocksAfterFiveFailuresBlocksRefreshAndRecoversAfterExpiry()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await LoginAsync(client, session.Auth.User.Email, "Incorrect-1!");
            await AssertProblemAsync(failed, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        }

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(session.Auth.User.Id);
        Assert.NotNull(user);
        Assert.True(await users.IsLockedOutAsync(user));
        Assert.InRange((user.LockoutEnd!.Value - DateTimeOffset.UtcNow).TotalMinutes, 14, 16);

        using var lockedLogin = await LoginAsync(client, session.Auth.User.Email, session.Password);
        await AssertProblemAsync(lockedLogin, HttpStatusCode.Unauthorized, "ACCOUNT_LOCKED");
        using var refresh = await RefreshAsync(client, session.Refresh);
        await AssertProblemAsync(refresh, HttpStatusCode.Unauthorized, "ACCOUNT_LOCKED");
        Assert.False(refresh.Headers.Contains("Set-Cookie"));
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.All(await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync(), t => Assert.True(t.IsRevoked));

        Assert.True((await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1))).Succeeded);
        using var recoveryClient = CreateClient();
        using var recovered = await LoginAsync(recoveryClient, session.Auth.User.Email, session.Password);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

    [Fact]
    public async Task SuccessfulLoginResetsFailureCountAndMissingAccountHasSameCredentialError()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using var failed = await LoginAsync(client, session.Auth.User.Email, "Incorrect-1!");
        using var missing = await LoginAsync(client, "missing@example.test", "Incorrect-1!");
        await AssertProblemAsync(failed, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        await AssertProblemAsync(missing, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        using var success = await LoginAsync(client, session.Auth.User.Email, session.Password);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(session.Auth.User.Id);
        Assert.NotNull(user);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public async Task ReuseRevokesAllPersistedSessionsWithoutAffectingAnotherUser()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        var other = await RegisterAsync(client);
        using var secondLogin = await LoginAsync(client, session.Auth.User.Email, session.Password);
        var secondRefresh = CookieToken(secondLogin);
        using var rotation = await RefreshAsync(client, session.Refresh);
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);
        var successor = CookieToken(rotation);

        using var reuse = await RefreshAsync(client, session.Refresh);
        await AssertProblemAsync(reuse, HttpStatusCode.Unauthorized, "REVOKED_REFRESH_TOKEN");
        Assert.False(reuse.Headers.Contains("Set-Cookie"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokens = await db.RefreshTokens.Where(t => t.UserId == session.Auth.User.Id).ToListAsync();
        Assert.Equal(3, tokens.Count);
        Assert.All(tokens, t => Assert.True(t.IsRevoked));
        Assert.DoesNotContain(tokens, t => t.TokenHash == session.Refresh || t.TokenHash == successor || t.TokenHash == secondRefresh);
        using var revokedSuccessor = await RefreshAsync(client, successor);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSuccessor.StatusCode);
        using var otherRefresh = await RefreshAsync(client, other.Refresh);
        Assert.Equal(HttpStatusCode.OK, otherRefresh.StatusCode);
    }

    [Fact]
    public async Task LogoutExpiresCookieWithMatchingFlagsAndRevokesPersistedToken()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/auth/logout", session.Auth.AccessToken);
        request.Headers.Add("Cookie", $"rt={session.Refresh}");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertCookieFlags(response);
        Assert.Contains("expires=Thu, 01 Jan 1970", Assert.Single(response.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
        using var refresh = await RefreshAsync(client, session.Refresh);
        await AssertProblemAsync(refresh, HttpStatusCode.Unauthorized, "REVOKED_REFRESH_TOKEN");
    }

    [Theory]
    [InlineData("https://attacker.example")]
    [InlineData("http://localhost:3000.attacker.example")]
    [InlineData("http://localhost:3000/path")]
    [InlineData("null")]
    public async Task UntrustedOriginCannotRotateCookieOrWriteProfile(string origin)
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Cookie", $"rt={session.Refresh}");
        using var response = await client.SendAsync(request);
        await AssertProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        using var patch = AuthorizedRequest(HttpMethod.Patch, "/api/v1/auth/me", session.Auth.AccessToken);
        patch.Headers.Add("Origin", origin);
        patch.Content = JsonContent.Create(new { displayName = "Attacker" });
        using var profile = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.Forbidden, profile.StatusCode);
        using var stillValid = await RefreshAsync(client, session.Refresh);
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
    }

    [Fact]
    public async Task AllowListedOriginSupportsCredentialedCorsPreflightAndRefresh()
    {
        using var client = CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        preflight.Headers.Add("Origin", "http://localhost:3000");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var preflightResponse = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, preflightResponse.StatusCode);
        Assert.Equal("http://localhost:3000", Assert.Single(preflightResponse.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(preflightResponse.Headers.GetValues("Access-Control-Allow-Credentials")));

        var session = await RegisterAsync(client);
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        using var refresh = await RefreshAsync(client, session.Refresh);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        AssertCookieFlags(refresh);
        Assert.Equal("http://localhost:3000", Assert.Single(refresh.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task DisallowedPreflightDoesNotGrantCorsAccess()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "https://attacker.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        using var response = await client.SendAsync(request);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task RateLimitCannotBeBypassedBySpoofingForwardedHeaderAndReturnsProblemAndRetryAfter()
    {
        using var client = CreateClient();
        for (var i = 0; i < 11; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { email = "absent@example.test", password = "Wrong-1!" }),
            };
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{i}");
            using var response = await client.SendAsync(request);
            if (i < 10) Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            else
            {
                await AssertProblemAsync(response, HttpStatusCode.TooManyRequests);
                var seconds = int.Parse(Assert.Single(response.Headers.GetValues("Retry-After")), CultureInfo.InvariantCulture);
                Assert.InRange(seconds, 1, 60);
            }
        }

        using var differentPeer = CreateClient();
        using var allowed = await LoginAsync(differentPeer, "absent@example.test", "Wrong-1!");
        Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
    }

    [Fact]
    public async Task ProfileUsesJwtSubjectPersistsChangesAndIgnoresPrivilegeEscalationFields()
    {
        using var client = CreateClient();
        var owner = await RegisterAsync(client);
        var other = await RegisterAsync(client);
        using var patch = AuthorizedRequest(HttpMethod.Patch, "/api/v1/auth/me", owner.Auth.AccessToken);
        patch.Content = JsonContent.Create(new
        {
            userId = other.Auth.User.Id,
            email = "attacker@example.test",
            roles = new[] { Roles.Admin },
            displayName = "Updated Chef",
            avatarUrl = "https://example.test/avatar.png",
            bio = "Updated bio",
        });
        using var response = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.NotNull(updated);
        Assert.Equal(owner.Auth.User.Id, updated.Id);
        Assert.Equal(owner.Auth.User.Email, updated.Email);
        Assert.DoesNotContain(Roles.Admin, updated.Roles);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stored = await users.FindByIdAsync(owner.Auth.User.Id);
        Assert.NotNull(stored);
        Assert.Equal("Updated Chef", stored.DisplayName);
        Assert.Equal("Updated bio", stored.Bio);
        Assert.Equal("https://example.test/avatar.png", stored.AvatarUrl);
        Assert.Equal("Security Chef", (await users.FindByIdAsync(other.Auth.User.Id))!.DisplayName);
    }

    [Fact]
    public async Task InvalidProfileDoesNotPersistAndAnonymousProfileIsRejected()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using var patch = AuthorizedRequest(HttpMethod.Patch, "/api/v1/auth/me", session.Auth.AccessToken);
        patch.Content = JsonContent.Create(new { displayName = "Should not save", avatarUrl = "javascript:alert(1)" });
        using var response = await client.SendAsync(patch);
        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR");
        using var get = AuthorizedRequest(HttpMethod.Get, "/api/v1/auth/me", session.Auth.AccessToken);
        using var persisted = await client.SendAsync(get);
        Assert.Equal("Security Chef", (await persisted.Content.ReadFromJsonAsync<AuthUserDto>())!.DisplayName);
        using var anonymous = await client.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = "Anonymous" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task RealJwtEnforcesAdminRoleAndRejectsTamperedToken()
    {
        using var client = CreateClient();
        var author = await RegisterAsync(client);
        using var anonymous = await client.PostAsJsonAsync("/api/v1/categories/", new { name = "Auth RBAC" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var forbiddenRequest = AuthorizedRequest(HttpMethod.Post, "/api/v1/categories/", author.Auth.AccessToken);
        forbiddenRequest.Content = JsonContent.Create(new { name = "Auth RBAC" });
        using var forbidden = await client.SendAsync(forbiddenRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var login = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var admin = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(admin);
        using var adminRequest = AuthorizedRequest(HttpMethod.Post, "/api/v1/categories/", admin.AccessToken);
        adminRequest.Content = JsonContent.Create(new { name = $"Auth RBAC {Guid.NewGuid():N}" });
        using var created = await client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var parts = author.Auth.AccessToken.Split('.');
        parts[1] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"sub\":\"admin\",\"role\":\"Admin\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var tamperedRequest = AuthorizedRequest(HttpMethod.Get, "/api/v1/auth/me", string.Join('.', parts));
        using var tampered = await client.SendAsync(tamperedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, tampered.StatusCode);
    }

    [Fact]
    public async Task AuthLogsExcludeCredentialsTokensAndPersonalProfileDataIncludingAdminSeed()
    {
        using var client = CreateClient();
        var session = await RegisterAsync(client);
        using var failed = await LoginAsync(client, session.Auth.User.Email, session.Password + "invalid");
        using var google = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = "private-google-credential" });
        Assert.Equal(HttpStatusCode.Unauthorized, google.StatusCode);
        var bio = $"Private biography {Guid.NewGuid():N}";
        using var patch = AuthorizedRequest(HttpMethod.Patch, "/api/v1/auth/me", session.Auth.AccessToken);
        patch.Content = JsonContent.Create(new { bio });
        using var profile = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        Assert.Contains(factory.Logs, entry => entry.MessageTemplate.Text.Contains("Seeded default admin", StringComparison.Ordinal));
        var rendered = string.Join('\n', factory.Logs.Select(RenderLog));
        foreach (var value in new[] { session.Password, session.Auth.User.Email, session.Auth.AccessToken, session.Refresh,
            factory.AdminEmail, factory.AdminPassword, bio, "private-google-credential" })
        {
            Assert.DoesNotContain(value, rendered, StringComparison.Ordinal);
        }
    }

    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
        var id = Interlocked.Increment(ref s_clientId);
        client.DefaultRequestHeaders.Add("X-Test-Client-IP", $"192.0.{id / 250}.{id % 250 + 1}");
        return client;
    }

    private static async Task<Session> RegisterAsync(HttpClient client)
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Security Chef",
            email = $"chef-{Guid.NewGuid():N}@example.test",
            password,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AssertCookieFlags(response);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("refreshToken", out _));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        return new Session(auth, CookieToken(response), password);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refresh)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"rt={refresh}");
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static void AssertCookieFlags(HttpResponseMessage response)
    {
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private static string CookieToken(HttpResponseMessage response) =>
        WebUtility.UrlDecode(Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0]["rt=".Length..]);

    private static string RenderLog(Serilog.Events.LogEvent entry) =>
        entry.RenderMessage(CultureInfo.InvariantCulture) + " " +
        string.Join(' ', entry.Properties.Select(property => property.ToString())) + " " + entry.Exception;

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? code = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        if (code is not null) Assert.Equal(code, problem.Type);
    }

    private sealed record Session(AuthResponseDto Auth, string Refresh, string Password);
}
