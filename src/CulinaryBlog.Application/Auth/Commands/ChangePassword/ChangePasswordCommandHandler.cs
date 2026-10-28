namespace CulinaryBlog.Application.Auth.Commands.ChangePassword;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Common.Exceptions;

public sealed class ChangePasswordCommandHandler(
    ICurrentUser currentUser,
    IIdentityService identityService,
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider) : ICommandHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Người dùng chưa được xác thực.", "UNAUTHORIZED");
        }

        await identityService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        // Sessions opened with the old password must not outlive it.
        await refreshTokenRepository.RevokeAllForUserAsync(userId, timeProvider.GetUtcNow(), cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
