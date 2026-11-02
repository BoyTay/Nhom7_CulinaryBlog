using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Common.Exceptions;
using FluentValidation;

namespace CulinaryBlog.Application.Users;

public sealed record GetManagedUsersQuery(string? Search, string? Role, string? Status, int Page = 1, int PageSize = 8)
    : IQuery<ManagedUserListDto>;

public sealed class GetManagedUsersQueryValidator : AbstractValidator<GetManagedUsersQuery>
{
    public GetManagedUsersQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Role).Must(role => role is null or Roles.Admin or Roles.Author);
        RuleFor(query => query.Status).Must(status => status is null or ManagedUserStatus.Active or ManagedUserStatus.Locked);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetManagedUsersQueryHandler(IUserAdministrationService users)
    : IQueryHandler<GetManagedUsersQuery, ManagedUserListDto>
{
    public Task<ManagedUserListDto> Handle(GetManagedUsersQuery request, CancellationToken cancellationToken) =>
        users.ListAsync(
            new ManagedUserFilter(request.Search?.Trim(), request.Role, request.Status, request.Page, request.PageSize),
            cancellationToken);
}

public sealed record ChangeUserRoleCommand(string UserId, string Role) : ICommand;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Role).Must(role => role is Roles.Admin or Roles.Author);
    }
}

public sealed class ChangeUserRoleCommandHandler(ICurrentUser currentUser, IUserAdministrationService users)
    : ICommandHandler<ChangeUserRoleCommand>
{
    public Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken) =>
        users.ChangeRoleAsync(ActorId.From(currentUser), request.UserId, request.Role, cancellationToken);
}

public sealed record SetUserLockCommand(string UserId, bool Locked, int? Days = null) : ICommand;

public sealed class SetUserLockCommandValidator : AbstractValidator<SetUserLockCommand>
{
    public SetUserLockCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Days).InclusiveBetween(1, 365).When(command => command.Locked);
        RuleFor(command => command.Days).Null().When(command => !command.Locked);
    }
}

public sealed class SetUserLockCommandHandler(ICurrentUser currentUser, IUserAdministrationService users)
    : ICommandHandler<SetUserLockCommand>
{
    public Task Handle(SetUserLockCommand request, CancellationToken cancellationToken) =>
        users.SetLockedAsync(ActorId.From(currentUser), request.UserId, request.Locked, request.Days, cancellationToken);
}

internal static class ActorId
{
    public static string From(ICurrentUser currentUser) =>
        string.IsNullOrWhiteSpace(currentUser.UserId)
            ? throw new UnauthorizedException("Người dùng chưa được xác thực.", "UNAUTHORIZED")
            : currentUser.UserId;
}
