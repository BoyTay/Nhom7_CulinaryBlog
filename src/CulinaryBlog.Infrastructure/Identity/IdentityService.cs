namespace CulinaryBlog.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IGoogleTokenValidator googleTokenValidator) : IIdentityService
{
    public async Task<AuthUserDto> RegisterAsync(
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException("Email này đã được sử dụng.", "EMAIL_ALREADY_EXISTS");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Code));
            throw new ConflictException($"Đăng ký không thành công: {errors}", "REGISTRATION_FAILED");
        }

        // Đảm bảo role Author tồn tại và gán cho user
        if (!await roleManager.RoleExistsAsync(Roles.Author))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Author));
        }

        await userManager.AddToRoleAsync(user, Roles.Author);

        return new AuthUserDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            [Roles.Author]);
    }

    public async Task<AuthUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(user);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);

        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            roles.ToList());
    }

    public async Task<AuthUserDto?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            roles.ToList());
    }

    public async Task<GoogleSignInResult> FindOrCreateGoogleUserAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        var payload = await googleTokenValidator.ValidateAsync(idToken, cancellationToken);

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedException("Email Google chưa được xác thực.", "GOOGLE_EMAIL_NOT_VERIFIED");
        }

        // Tìm kiếm theo Google login trước (liên kết bằng Google sub)
        var user = await userManager.FindByLoginAsync("Google", payload.Subject);
        var isNewUser = false;

        if (user is null)
        {
            // Kiểm tra xem email đã tồn tại chưa
            user = await userManager.FindByEmailAsync(payload.Email);

            if (user is null)
            {
                // Tạo tài khoản mới từ Google profile
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    AvatarUrl = payload.Picture,
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Code));
                    throw new ConflictException($"Không thể tạo tài khoản từ Google: {errors}", "GOOGLE_USER_CREATION_FAILED");
                }

                if (!await roleManager.RoleExistsAsync(Roles.Author))
                {
                    await roleManager.CreateAsync(new IdentityRole(Roles.Author));
                }

                await userManager.AddToRoleAsync(user, Roles.Author);
                isNewUser = true;
            }
            else
            {
                var needsUpdate = false;
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    needsUpdate = true;
                }

                if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(payload.Picture))
                {
                    user.AvatarUrl = payload.Picture;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(user);
                }
            }

            // Liên kết tài khoản với Google login qua Google sub
            var addLoginResult = await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
            if (!addLoginResult.Succeeded)
            {
                var errors = string.Join("; ", addLoginResult.Errors.Select(e => e.Code));
                throw new ConflictException($"Không thể liên kết Google login: {errors}", "GOOGLE_LOGIN_LINK_FAILED");
            }
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            // Same code as password login and refresh, so clients handle a locked account in one place.
            throw new UnauthorizedException("Tài khoản đã bị tạm khóa do vi phạm hoặc nhập sai nhiều lần.", "ACCOUNT_LOCKED");
        }

        var roles = await userManager.GetRolesAsync(user);

        return new GoogleSignInResult(
            new AuthUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                user.Bio,
                roles.ToList()),
            isNewUser);
    }

    public async Task<AuthUserDto> UpdateProfileAsync(
        string userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            user.DisplayName = displayName;
        }

        if (avatarUrl is not null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        }

        if (bio is not null)
        {
            user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Code));
            throw new ConflictException($"Cập nhật thông tin thất bại: {errors}", "UPDATE_PROFILE_FAILED");
        }

        var roles = await userManager.GetRolesAsync(user);

        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            roles.ToList());
    }

    public async Task<bool> IsLockedOutAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return false;
        }

        return await userManager.IsLockedOutAsync(user);
    }

    public async Task ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        if (!await userManager.HasPasswordAsync(user))
        {
            throw new ConflictException(
                "Tài khoản đăng nhập bằng Google chưa có mật khẩu để thay đổi.",
                "PASSWORD_NOT_SET");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            throw new ApplicationValidationException(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["Mật khẩu hiện tại không đúng."],
            });
        }

        throw new ApplicationValidationException(new Dictionary<string, string[]>
        {
            ["newPassword"] = result.Errors.Select(e => e.Description).ToArray(),
        });
    }

    public async Task<PasswordResetTicket?> CreatePasswordResetTicketAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user?.Email is null)
        {
            return null;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return new PasswordResetTicket(user.Email, user.DisplayName, token);
    }

    public async Task<string> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        // Unknown email and bad token share one answer so the endpoint cannot be used to probe accounts.
        var invalidLink = new ConflictException(
            "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.",
            "INVALID_RESET_TOKEN");

        var user = await userManager.FindByEmailAsync(email) ?? throw invalidLink;

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                throw invalidLink;
            }

            throw new ApplicationValidationException(new Dictionary<string, string[]>
            {
                ["newPassword"] = result.Errors.Select(e => e.Description).ToArray(),
            });
        }

        // Proving control of the mailbox also clears a brute-force lockout.
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);

        return user.Id;
    }
}
