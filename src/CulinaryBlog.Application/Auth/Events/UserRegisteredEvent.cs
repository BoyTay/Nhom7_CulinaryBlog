namespace CulinaryBlog.Application.Auth.Events;

using MediatR;

/// <summary>
/// Domain/Application Event được phát hành khi người dùng đăng ký tài khoản thành công.
/// Được sử dụng làm contract đầu vào cho Background Job gửi Welcome Email (Issue #33 do Kiên phụ trách).
/// </summary>
public sealed record UserRegisteredEvent(
    string UserId,
    string Email,
    string DisplayName,
    DateTimeOffset OccurredAt) : INotification;

