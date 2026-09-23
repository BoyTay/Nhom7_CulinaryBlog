namespace CulinaryBlog.Application.Abstractions.Identity;

public interface IJwtService
{
    string GenerateAccessToken(string userId, string email, string displayName, IReadOnlyList<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
}
