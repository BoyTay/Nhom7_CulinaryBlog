namespace CulinaryBlog.Application.Auth.Commands.Register;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Auth.Events;
using CulinaryBlog.Domain.Users;
using MediatR;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository,
    IPublisher publisher) : ICommandHandler<RegisterCommand, TokenResult>
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

        await publisher.Publish(
            new UserRegisteredEvent(
                user.Id,
                user.Email,
                user.DisplayName,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return new TokenResult(
            accessToken,
            rawRefreshToken,
            AccessTokenExpiresInSeconds,
            user);
    }
}
