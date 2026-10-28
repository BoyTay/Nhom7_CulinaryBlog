namespace CulinaryBlog.Application.Auth.Commands.ResetPassword;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;

public sealed class ResetPasswordCommandHandler(
    IIdentityService identityService,
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider) : ICommandHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = await identityService.ResetPasswordAsync(
            request.Email.Trim(),
            request.Token.Trim(),
            request.NewPassword,
            cancellationToken);

        await refreshTokenRepository.RevokeAllForUserAsync(userId, timeProvider.GetUtcNow(), cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
