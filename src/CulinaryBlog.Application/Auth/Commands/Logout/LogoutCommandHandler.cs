namespace CulinaryBlog.Application.Auth.Commands.Logout;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;

public sealed class LogoutCommandHandler(
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository) : ICommandHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = jwtService.HashToken(request.RefreshToken);
        var token = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke(DateTimeOffset.UtcNow);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
