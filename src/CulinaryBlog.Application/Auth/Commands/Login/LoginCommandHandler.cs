namespace CulinaryBlog.Application.Auth.Commands.Login;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Users;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<LoginCommand, TokenResult>
{
    private const int AccessTokenExpiresInSeconds = 15 * 60; // 15 mins

    public async Task<TokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (await identityService.IsLockedOutAsync(request.Email, cancellationToken))
        {
            throw new UnauthorizedException(
                "Tài khoản đã bị tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.",
                "ACCOUNT_LOCKED");
        }

        var user = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(
                "Email hoặc mật khẩu không chính xác.",
                "INVALID_CREDENTIALS");
        }

        var accessToken = jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Roles);

        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            request.IpAddress);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResult(
            accessToken,
            rawRefreshToken,
            AccessTokenExpiresInSeconds,
            user);
    }
}
