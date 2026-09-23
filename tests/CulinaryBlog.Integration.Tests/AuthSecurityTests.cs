using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Integration.Tests;

public sealed class AuthSecurityTests
{
    private const string ValidSecretKey = "this-is-a-secure-secret-key-at-least-32-chars-long!";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short-key")]
    public void JwtServiceThrowsInvalidOperationExceptionWhenSecretKeyMissingOrTooShort(string? secretKey)
    {
        var inMemory = new Dictionary<string, string?>();
        if (secretKey is not null)
        {
            inMemory["Jwt:SecretKey"] = secretKey;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var jwtService = new JwtService(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            jwtService.GenerateAccessToken("user-1", "user@test.com", "Test User", [Roles.Author]));
    }

    [Fact]
    public void JwtServiceGeneratesValidTokenWithExpectedClaimsAndExpiry()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = ValidSecretKey,
                ["Jwt:Issuer"] = "CulinaryBlog.API",
                ["Jwt:Audience"] = "CulinaryBlog.Web",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var tokenString = jwtService.GenerateAccessToken("user-123", "author@culinary.com", "Chef John", [Roles.Author]);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));

        var jwt = handler.ReadJwtToken(tokenString);
        Assert.Equal("CulinaryBlog.API", jwt.Issuer);
        Assert.Contains("CulinaryBlog.Web", jwt.Audiences);
        Assert.Equal("user-123", jwt.Subject);
        Assert.Equal("author@culinary.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Chef John", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(Roles.Author, jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(14));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void JwtServiceGeneratesAndHashesRefreshTokenWithSha256()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = ValidSecretKey,
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var refreshToken = jwtService.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.True(refreshToken.Length >= 40);

        var tokenHash = jwtService.HashToken(refreshToken);
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        Assert.Equal(expectedHash, tokenHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-jwt-token")]
    public async Task GoogleTokenValidatorThrowsUnauthorizedExceptionWhenTokenInvalidOrFake(string fakeToken)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client-id.apps.googleusercontent.com",
            })
            .Build();

        var httpClientFactory = new DummyHttpClientFactory();
        var validator = new GoogleTokenValidator(httpClientFactory, configuration, NullLogger<GoogleTokenValidator>.Instance);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            validator.ValidateAsync(fakeToken));

        Assert.Equal("INVALID_GOOGLE_TOKEN", exception.Code);
    }

    [Fact]
    public async Task GoogleTokenValidatorThrowsUnauthorizedExceptionWhenIssuerNotGoogle()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client-id.apps.googleusercontent.com",
            })
            .Build();

        // Create an untrusted JWT signed with arbitrary issuer
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = "https://untrusted-issuer.com",
            Audience = "test-client-id.apps.googleusercontent.com",
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", "google-sub-123"),
                new Claim("email", "fake@test.com"),
                new Claim("email_verified", "true"),
            ]),
        };

        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var fakeToken = tokenHandler.WriteToken(securityToken);

        var httpClientFactory = new DummyHttpClientFactory();
        var validator = new GoogleTokenValidator(httpClientFactory, configuration, NullLogger<GoogleTokenValidator>.Instance);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            validator.ValidateAsync(fakeToken));

        Assert.Equal("INVALID_GOOGLE_TOKEN", exception.Code);
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
