namespace CulinaryBlog.Application.Auth.Commands.GoogleLogin;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record GoogleLoginCommand(
    string IdToken,
    string? IpAddress = null) : ICommand<TokenResult>;
