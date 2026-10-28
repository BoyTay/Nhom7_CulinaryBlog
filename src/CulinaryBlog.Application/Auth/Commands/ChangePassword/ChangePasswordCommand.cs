namespace CulinaryBlog.Application.Auth.Commands.ChangePassword;

using CulinaryBlog.Application.Abstractions.Messaging;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : ICommand;
