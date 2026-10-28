namespace CulinaryBlog.Infrastructure.Identity;

using System.Text.Encodings.Web;
using CulinaryBlog.Application.Abstractions.Identity;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

public sealed class SmtpPasswordResetEmailSender(IConfiguration configuration) : IPasswordResetEmailSender
{
    public const string ResetPagePath = "/reset-password";

    public async Task SendAsync(PasswordResetTicket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Email SMTP host is not configured.");
        }

        var port = configuration.GetValue<int?>("Email:SmtpPort")
            ?? throw new InvalidOperationException("Email SMTP port is not configured.");
        var securitySetting = configuration["Email:SmtpSecurity"] ?? nameof(SecureSocketOptions.StartTls);
        if (!Enum.TryParse<SecureSocketOptions>(securitySetting, true, out var security)
            || !Enum.IsDefined(security))
        {
            throw new InvalidOperationException(
                $"Email SMTP security mode '{securitySetting}' is not supported.");
        }

        var resetUri = BuildResetUri(configuration["PublicSite:BaseUrl"], ticket);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            configuration["Email:FromName"] ?? "Culinary Blog",
            configuration["Email:FromAddress"] ?? "noreply@culinaryblog.local"));
        message.To.Add(MailboxAddress.Parse(ticket.Email));
        message.Subject = "Đặt lại mật khẩu Culinary Blog";

        var safeDisplayName = HtmlEncoder.Default.Encode(ticket.DisplayName);
        var safeResetUrl = HtmlEncoder.Default.Encode(resetUri.AbsoluteUri);
        message.Body = new BodyBuilder
        {
            TextBody = $"Xin chào {ticket.DisplayName},\n\n"
                + $"Mở liên kết sau để đặt lại mật khẩu (hiệu lực 30 phút, dùng một lần):\n{resetUri.AbsoluteUri}\n\n"
                + "Nếu bạn không yêu cầu, hãy bỏ qua email này.",
            HtmlBody = $"""
                <!doctype html>
                <html lang="vi">
                <body>
                  <h1>Đặt lại mật khẩu</h1>
                  <p>Xin chào {safeDisplayName},</p>
                  <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                     Liên kết có hiệu lực trong 30 phút và chỉ dùng được một lần.</p>
                  <p><a href="{safeResetUrl}">Đặt lại mật khẩu</a></p>
                  <p>Nếu bạn không yêu cầu, hãy bỏ qua email này; mật khẩu hiện tại vẫn giữ nguyên.</p>
                </body>
                </html>
                """,
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, security, cancellationToken);

        var username = configuration["Email:SmtpUsername"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            if (!client.IsSecure)
            {
                throw new InvalidOperationException(
                    "SMTP authentication is not allowed without a secure connection.");
            }

            var password = configuration["Email:SmtpPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Email SMTP password is required when a username is configured.");
            }

            await client.AuthenticateAsync(username, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public static Uri BuildResetUri(string? siteBaseUrl, PasswordResetTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (!Uri.TryCreate(siteBaseUrl, UriKind.Absolute, out var siteUri)
            || siteUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Public site base URL must be an absolute HTTP or HTTPS URL.");
        }

        var query = $"email={Uri.EscapeDataString(ticket.Email)}&token={Uri.EscapeDataString(ticket.Token)}";
        return new UriBuilder(siteUri)
        {
            Path = siteUri.AbsolutePath.TrimEnd('/') + ResetPagePath,
            Query = query,
        }.Uri;
    }
}
