namespace CulinaryBlog.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public sealed class GoogleTokenValidator(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    private static readonly string[] ValidIssuers = ["https://accounts.google.com", "accounts.google.com"];
    private const string GoogleJwksUrl = "https://www.googleapis.com/oauth2/v3/certs";

    private static readonly Action<ILogger, Exception?> LogFailedToReadToken =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7001, "FailedToReadToken"), "Failed to read JWT token.");

    private static readonly Action<ILogger, Exception?> LogJwksEndpointUnreachable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7002, "JwksUnreachable"), "Could not reach Google JWKS endpoint.");

    private static readonly Action<ILogger, Exception?> LogSignatureValidationFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7003, "SignatureValidationFailed"), "Google token signature validation failed.");

    private static readonly Action<ILogger, Exception?> LogUnexpectedError =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7004, "UnexpectedError"), "Unexpected error during Google token verification.");

    public async Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedException("Google token không được để trống.", "INVALID_GOOGLE_TOKEN");
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(idToken))
        {
            throw new UnauthorizedException("Google token không đúng định dạng JWT.", "INVALID_GOOGLE_TOKEN");
        }

        JwtSecurityToken jwt;
        try
        {
            jwt = handler.ReadJwtToken(idToken);
        }
        catch (Exception ex)
        {
            LogFailedToReadToken(logger, ex);
            throw new UnauthorizedException("Không thể giải mã Google token.", "INVALID_GOOGLE_TOKEN");
        }

        // 1. Kiểm tra Issuer
        if (!ValidIssuers.Contains(jwt.Issuer, StringComparer.Ordinal))
        {
            throw new UnauthorizedException($"Google token có issuer '{jwt.Issuer}' không hợp lệ.", "INVALID_GOOGLE_TOKEN");
        }

        // 2. Kiểm tra Audience nếu được cấu hình
        var configuredClientId = configuration["Authentication:Google:ClientId"];
        if (!string.IsNullOrWhiteSpace(configuredClientId) &&
            !jwt.Audiences.Contains(configuredClientId, StringComparer.Ordinal))
        {
            throw new UnauthorizedException("Google token không đúng Client ID cấu hình.", "INVALID_GOOGLE_TOKEN");
        }

        // 3. Kiểm tra Expiration
        if (jwt.ValidTo < DateTime.UtcNow.AddMinutes(-1))
        {
            throw new UnauthorizedException("Google token đã hết hạn.", "INVALID_GOOGLE_TOKEN");
        }

        // 4. Xác minh chữ ký mật mã (Cryptographic Signature Verification) với Google JWKS
        try
        {
            var httpClient = httpClientFactory.CreateClient("GoogleJwks");
            var jwksResponse = await httpClient.GetStringAsync(new Uri(GoogleJwksUrl), cancellationToken);
            var keySet = new JsonWebKeySet(jwksResponse);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = ValidIssuers,
                ValidateAudience = !string.IsNullOrWhiteSpace(configuredClientId),
                ValidAudience = configuredClientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keySet.GetSigningKeys(),
            };

            handler.ValidateToken(idToken, validationParameters, out _);
        }
        catch (HttpRequestException ex)
        {
            LogJwksEndpointUnreachable(logger, ex);
            throw new UnauthorizedException("Không thể xác minh chữ ký Google token do lỗi mạng.", "GOOGLE_SERVICE_UNAVAILABLE");
        }
        catch (SecurityTokenException ex)
        {
            LogSignatureValidationFailed(logger, ex);
            throw new UnauthorizedException("Chữ ký của Google token không hợp lệ.", "INVALID_GOOGLE_TOKEN");
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            LogUnexpectedError(logger, ex);
            throw new UnauthorizedException("Xác thực Google token thất bại.", "INVALID_GOOGLE_TOKEN");
        }

        // 5. Trích xuất claims
        var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            throw new UnauthorizedException("Google token thiếu thông tin subject (sub).", "INVALID_GOOGLE_TOKEN");
        }

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedException("Google token không chứa email.", "INVALID_GOOGLE_TOKEN");
        }

        var emailVerifiedStr = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
        var emailVerified = bool.TryParse(emailVerifiedStr, out var ev) && ev;
        if (!emailVerified)
        {
            throw new UnauthorizedException("Email Google chưa được xác thực.", "GOOGLE_EMAIL_NOT_VERIFIED");
        }

        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? email.Split('@')[0];
        var picture = jwt.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        return new GoogleUserPayload(sub, email, name, picture, emailVerified);
    }
}
