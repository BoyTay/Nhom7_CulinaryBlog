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
    RoleManager<IdentityRole> roleManager) : IIdentityService
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
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
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

    public async Task<AuthUserDto> FindOrCreateGoogleUserAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        // Phân tích Google ID token
        string email;
        string name;
        string? picture = null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(idToken))
            {
                var jwt = handler.ReadJwtToken(idToken);
                email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                    ?? throw new UnauthorizedException("Google token không chứa email.", "INVALID_GOOGLE_TOKEN");
                name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? email.Split('@')[0];
                picture = jwt.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
            }
            else
            {
                throw new UnauthorizedException("Google token không đúng định dạng.", "INVALID_GOOGLE_TOKEN");
            }
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            throw new UnauthorizedException("Không thể xác thực Google token.", "INVALID_GOOGLE_TOKEN");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                AvatarUrl = picture,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new ConflictException($"Không thể tạo tài khoản từ Google: {errors}", "GOOGLE_USER_CREATION_FAILED");
            }

            if (!await roleManager.RoleExistsAsync(Roles.Author))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Author));
            }

            await userManager.AddToRoleAsync(user, Roles.Author);
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
            user.AvatarUrl = avatarUrl;
        }

        if (bio is not null)
        {
            user.Bio = bio;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
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
}
