namespace CulinaryBlog.Application.Auth.Commands.Logout;

using CulinaryBlog.Application.Abstractions.Messaging;

public sealed record LogoutCommand(string? RefreshToken) : ICommand;
