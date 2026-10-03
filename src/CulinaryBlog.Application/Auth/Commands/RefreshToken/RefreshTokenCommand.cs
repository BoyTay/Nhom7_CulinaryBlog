namespace CulinaryBlog.Application.Auth.Commands.RefreshToken;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : ICommand<TokenResult>;
