namespace CulinaryBlog.Application.Auth.Commands.RefreshToken;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Users;

public sealed class RefreshTokenCommandHandler(
    IIdentityService identityService,
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<RefreshTokenCommand, TokenResult>
{
    private const int AccessTokenExpiresInSeconds = 15 * 60; // 15 mins

    public async Task<TokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtService.HashToken(request.RefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException("Refresh token không hợp lệ.", "INVALID_REFRESH_TOKEN");
        }

        if (existingToken.IsRevoked)
        {
            // Token reuse detection: thu hồi toàn bộ token của user để bảo vệ tài khoản
            await refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId, DateTimeOffset.UtcNow, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(
                "Phát hiện refresh token đã bị thu hồi hoặc sử dụng lại trái phép. Toàn bộ phiên đăng nhập đã bị hủy vì lý do bảo mật.",
                "REVOKED_REFRESH_TOKEN");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.", "EXPIRED_REFRESH_TOKEN");
        }

        var user = await identityService.FindByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedException("Tài khoản người dùng không tồn tại.", "USER_NOT_FOUND");
        }

        // Token Rotation: tạo token mới và thu hồi token cũ
        var newRawRefreshToken = jwtService.GenerateRefreshToken();
        var newTokenHash = jwtService.HashToken(newRawRefreshToken);

        var newRefreshToken = RefreshToken.Create(
            existingToken.UserId,
            newTokenHash,
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            request.IpAddress);

        existingToken.Revoke(DateTimeOffset.UtcNow, newTokenHash);

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var newAccessToken = jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Roles);

        return new TokenResult(
            newAccessToken,
            newRawRefreshToken,
            AccessTokenExpiresInSeconds,
            user);
    }
}
