namespace CulinaryBlog.Application.Auth.Commands.ResetPassword;

using CulinaryBlog.Application.Abstractions.Messaging;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : ICommand;
