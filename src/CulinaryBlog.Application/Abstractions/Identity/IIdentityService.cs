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

    // IsNewUser lets the caller raise UserRegisteredEvent exactly once, as email registration does.
    Task<GoogleSignInResult> FindOrCreateGoogleUserAsync(
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

    Task ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    // Returns null when no account uses the email, so callers can answer identically either way.
    Task<PasswordResetTicket?> CreatePasswordResetTicketAsync(
        string email,
        CancellationToken cancellationToken = default);

    // Returns the id of the user whose password was reset.
    Task<string> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
