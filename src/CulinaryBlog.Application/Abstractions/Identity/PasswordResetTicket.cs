namespace CulinaryBlog.Application.Abstractions.Identity;

public sealed record PasswordResetTicket(string Email, string DisplayName, string Token);
