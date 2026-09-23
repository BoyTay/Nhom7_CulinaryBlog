namespace CulinaryBlog.Application.Auth.DTOs;

public sealed record AuthUserDto(
    string Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string> Roles);

public sealed record AuthResponseDto(
    string AccessToken,
    int ExpiresIn,
    AuthUserDto User);

public sealed record TokenResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthUserDto User);
