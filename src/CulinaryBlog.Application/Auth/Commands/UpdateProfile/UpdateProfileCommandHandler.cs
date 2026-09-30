namespace CulinaryBlog.Application.Auth.Commands.UpdateProfile;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;

public sealed class UpdateProfileCommandHandler(
    ICurrentUser currentUser,
    IIdentityService identityService) : ICommandHandler<UpdateProfileCommand, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Người dùng chưa được xác thực.", "UNAUTHORIZED");
        }

        return await identityService.UpdateProfileAsync(
            userId,
            request.DisplayName,
            request.AvatarUrl,
            request.Bio,
            cancellationToken);
    }
}
