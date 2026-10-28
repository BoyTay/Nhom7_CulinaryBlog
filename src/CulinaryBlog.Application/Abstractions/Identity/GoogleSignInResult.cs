namespace CulinaryBlog.Application.Abstractions.Identity;

using CulinaryBlog.Application.Auth.DTOs;

public sealed record GoogleSignInResult(AuthUserDto User, bool IsNewUser);
