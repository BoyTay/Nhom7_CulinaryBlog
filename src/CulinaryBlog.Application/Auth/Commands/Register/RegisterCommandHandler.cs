namespace CulinaryBlog.Application.Auth.Commands.Register;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Domain.Users;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<RegisterCommand, TokenResult>
{
    private const int AccessTokenExpiresInSeconds = 15 * 60; // 15 mins

    public async Task<TokenResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.RegisterAsync(
            request.DisplayName,
            request.Email,
            request.Password,
            cancellationToken);

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
