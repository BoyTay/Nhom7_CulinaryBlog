namespace CulinaryBlog.Application.Abstractions.Identity;

public interface IGoogleTokenValidator
{
    Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

public sealed record GoogleUserPayload(
    string Subject,
    string Email,
    string Name,
    string? Picture,
    bool EmailVerified);
