namespace CulinaryBlog.Application.Auth.Commands.ForgotPassword;

using CulinaryBlog.Application.Abstractions.Messaging;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
