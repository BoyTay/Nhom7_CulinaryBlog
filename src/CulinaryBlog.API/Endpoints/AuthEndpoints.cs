namespace CulinaryBlog.API.Endpoints;

using CulinaryBlog.Application.Auth.Commands.GoogleLogin;
using CulinaryBlog.Application.Auth.Commands.Login;
using CulinaryBlog.Application.Auth.Commands.Logout;
using CulinaryBlog.Application.Auth.Commands.RefreshToken;
using CulinaryBlog.Application.Auth.Commands.Register;
using CulinaryBlog.Application.Auth.Commands.UpdateProfile;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "rt";
    private const string AuthRoutePrefix = "/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(AuthRoutePrefix)
            .WithTags("Auth")
            .RequireRateLimiting("auth-rate-limit");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = GetClientIp(httpContext);
            var command = new RegisterCommand(request.DisplayName, request.Email, request.Password, ip);
            var result = await sender.Send(command, ct);

            SetRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Created(
                "/api/v1/auth/me",
                new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.User));
        })
        .WithName("Register")
        .WithSummary("Đăng ký tài khoản người dùng mới")
        .WithDescription("Tạo tài khoản mới, cấp access token 15 phút và refresh token trong HttpOnly secure cookie.")
        .Produces<AuthResponseDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = GetClientIp(httpContext);
            var command = new LoginCommand(request.Email, request.Password, ip);
            var result = await sender.Send(command, ct);

            SetRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Ok(new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.User));
        })
        .WithName("Login")
        .WithSummary("Đăng nhập bằng email và mật khẩu")
        .WithDescription("Xác thực thông tin đăng nhập, cấp access token 15 phút và refresh token trong HttpOnly secure cookie.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/google", async (
            [FromBody] GoogleLoginRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = GetClientIp(httpContext);
            var command = new GoogleLoginCommand(request.IdToken, ip);
            var result = await sender.Send(command, ct);

            SetRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Ok(new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.User));
        })
        .WithName("GoogleLogin")
        .WithSummary("Đăng nhập bằng Google ID token")
        .WithDescription("Xác thực Google ID token, tự động tạo tài khoản nếu chưa tồn tại, cấp access token 15 phút và refresh token cookie.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest? bodyRequest,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName]
                ?? bodyRequest?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Results.Unauthorized();
            }

            var ip = GetClientIp(httpContext);
            var command = new RefreshTokenCommand(refreshToken, ip);
            var result = await sender.Send(command, ct);

            SetRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Ok(new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.User));
        })
        .WithName("RefreshToken")
        .WithSummary("Làm mới access token")
        .WithDescription("Sử dụng refresh token từ HttpOnly cookie để cấp access token mới và xoay vòng refresh token.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", async (
            [FromBody] LogoutRequest? bodyRequest,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName]
                ?? bodyRequest?.RefreshToken;

            var command = new LogoutCommand(refreshToken);
            await sender.Send(command, ct);

            DeleteRefreshTokenCookie(httpContext);

            return Results.NoContent();
        })
        .WithName("Logout")
        .RequireAuthorization()
        .WithSummary("Đăng xuất tài khoản")
        .WithDescription("Thu hồi refresh token và xóa HttpOnly refresh cookie.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCurrentUserQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetCurrentUser")
        .RequireAuthorization()
        .WithSummary("Lấy thông tin người dùng hiện tại")
        .WithDescription("Trả về thông tin hồ sơ của người dùng đang đăng nhập.")
        .Produces<AuthUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/me", async (
            [FromBody] UpdateProfileRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new UpdateProfileCommand(request.DisplayName, request.AvatarUrl, request.Bio);
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("UpdateProfile")
        .RequireAuthorization()
        .WithSummary("Cập nhật hồ sơ người dùng")
        .WithDescription("Cập nhật tên hiển thị, ảnh đại diện hoặc tiểu sử của người dùng.")
        .Produces<AuthUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return endpoints;
    }

    private static string? GetClientIp(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/v1/auth",
        };

        httpContext.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private static void DeleteRefreshTokenCookie(HttpContext httpContext)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
        };

        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
    }
}

public sealed record RegisterRequest(string DisplayName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record GoogleLoginRequest(string IdToken);
public sealed record RefreshTokenRequest(string? RefreshToken);
public sealed record LogoutRequest(string? RefreshToken);
public sealed record UpdateProfileRequest(string? DisplayName, string? AvatarUrl, string? Bio);
