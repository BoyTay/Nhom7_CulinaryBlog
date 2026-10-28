namespace CulinaryBlog.API.Endpoints;

using CulinaryBlog.API.Security;
using CulinaryBlog.Application.Auth.Commands.ChangePassword;
using CulinaryBlog.Application.Auth.Commands.ForgotPassword;
using CulinaryBlog.Application.Auth.Commands.GoogleLogin;
using CulinaryBlog.Application.Auth.Commands.Login;
using CulinaryBlog.Application.Auth.Commands.Logout;
using CulinaryBlog.Application.Auth.Commands.RefreshToken;
using CulinaryBlog.Application.Auth.Commands.Register;
using CulinaryBlog.Application.Auth.Commands.ResetPassword;
using CulinaryBlog.Application.Auth.Commands.UpdateProfile;
using CulinaryBlog.Application.Auth.Commands.UploadAvatar;
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
            .AddEndpointFilter<AuthOriginFilter>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
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

        group.MapPost("/me/avatar", async (
            IFormFile? file,
            ISender sender,
            CancellationToken ct) =>
        {
            if (file is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Vui lòng chọn một tệp ảnh.",
                    type: "FILE_REQUIRED");
            }

            await using var content = file.OpenReadStream();
            var result = await sender.Send(new UploadAvatarCommand(content, file.Length, file.ContentType), ct);
            return Results.Ok(result);
        })
        .WithName("UploadAvatar")
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithSummary("Tải ảnh đại diện lên MinIO")
        .WithDescription("Nhận ảnh JPEG/PNG/WebP/AVIF tối đa 5 MB, kiểm tra chữ ký tệp, lưu vào MinIO và cập nhật avatar.")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<AuthUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);

            // Every refresh token was revoked, so the browser must sign in again with the new password.
            DeleteRefreshTokenCookie(httpContext);

            return Results.Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới." });
        })
        .WithName("ChangePassword")
        .RequireAuthorization()
        .WithSummary("Đổi mật khẩu tài khoản")
        .WithDescription("Kiểm tra mật khẩu hiện tại, đặt mật khẩu mới và thu hồi mọi refresh token của tài khoản.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ForgotPasswordCommand(request.Email), ct);

            // Same answer whether or not the email is registered, to prevent account enumeration.
            return Results.Accepted(value: new
            {
                message = "Nếu email đã đăng ký, chúng tôi đã gửi liên kết đặt lại mật khẩu. Vui lòng kiểm tra hộp thư.",
            });
        })
        .WithName("ForgotPassword")
        .WithSummary("Yêu cầu liên kết đặt lại mật khẩu")
        .WithDescription("Gửi email chứa liên kết đặt lại mật khẩu (hiệu lực 30 phút, dùng một lần). Phản hồi không tiết lộ email có tồn tại hay không.")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
            return Results.Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới." });
        })
        .WithName("ResetPassword")
        .WithSummary("Đặt lại mật khẩu bằng liên kết trong email")
        .WithDescription("Xác thực mã đặt lại, cập nhật mật khẩu, mở khóa tài khoản và thu hồi mọi refresh token.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
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
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
