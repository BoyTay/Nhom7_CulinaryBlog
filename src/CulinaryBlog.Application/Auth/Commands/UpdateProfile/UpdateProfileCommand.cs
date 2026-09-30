namespace CulinaryBlog.Application.Auth.Commands.UpdateProfile;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record UpdateProfileCommand(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio) : ICommand<AuthUserDto>;
