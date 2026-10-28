namespace CulinaryBlog.Application.Auth.Commands.UploadAvatar;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record UploadAvatarCommand(
    Stream Content,
    long ContentLength,
    string ContentType) : ICommand<AuthUserDto>;
