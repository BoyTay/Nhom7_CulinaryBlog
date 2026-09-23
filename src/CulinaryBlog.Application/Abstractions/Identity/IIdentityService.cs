namespace CulinaryBlog.Application.Abstractions.Identity;

using CulinaryBlog.Application.Auth.DTOs;

public interface IIdentityService
{
    Task<AuthUserDto> RegisterAsync(
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto> FindOrCreateGoogleUserAsync(
        string idToken,
        CancellationToken cancellationToken = default);

    Task<AuthUserDto> UpdateProfileAsync(
        string userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken cancellationToken = default);

    Task<bool> IsLockedOutAsync(
        string email,
        CancellationToken cancellationToken = default);
}
