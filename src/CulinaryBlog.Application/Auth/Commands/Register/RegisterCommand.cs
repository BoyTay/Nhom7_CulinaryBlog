namespace CulinaryBlog.Application.Auth.Commands.Register;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record RegisterCommand(
    string DisplayName,
    string Email,
    string Password,
    string? IpAddress = null) : ICommand<TokenResult>;
