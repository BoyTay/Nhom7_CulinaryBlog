namespace CulinaryBlog.Application.Auth.Commands.Login;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null) : ICommand<TokenResult>;
