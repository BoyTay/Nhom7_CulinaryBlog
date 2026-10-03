namespace CulinaryBlog.Application.Auth.Queries.GetCurrentUser;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IIdentityService identityService) : IQueryHandler<GetCurrentUserQuery, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Người dùng chưa được xác thực.", "UNAUTHORIZED");
        }

        var user = await identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Không tìm thấy thông tin người dùng.", "USER_NOT_FOUND");
        }

        return user;
    }
}
