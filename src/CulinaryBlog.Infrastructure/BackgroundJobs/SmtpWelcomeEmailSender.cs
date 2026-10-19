using System.Text.Encodings.Web;
using CulinaryBlog.Application.Abstractions.Jobs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class SmtpWelcomeEmailSender(IConfiguration configuration) : IWelcomeEmailSender
{
    public async Task SendAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Email SMTP host is not configured.");
        }

        var port = configuration.GetValue<int?>("Email:SmtpPort")
            ?? throw new InvalidOperationException("Email SMTP port is not configured.");
        var fromAddress = configuration["Email:FromAddress"] ?? "noreply@culinaryblog.local";
        var fromName = configuration["Email:FromName"] ?? "Culinary Blog";
        var securitySetting = configuration["Email:SmtpSecurity"] ?? nameof(SecureSocketOptions.StartTls);
        if (!Enum.TryParse<SecureSocketOptions>(securitySetting, true, out var security)
            || !Enum.IsDefined(security))
        {
            throw new InvalidOperationException(
                $"Email SMTP security mode '{securitySetting}' is not supported.");
        }

        var siteBaseUrl = configuration["PublicSite:BaseUrl"];
        if (!Uri.TryCreate(siteBaseUrl, UriKind.Absolute, out var siteUri)
            || siteUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Public site base URL must be an absolute HTTP or HTTPS URL.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Chào mừng bạn đến với Culinary Blog";

        var safeDisplayName = HtmlEncoder.Default.Encode(displayName);
        var siteUrl = HtmlEncoder.Default.Encode(siteUri.AbsoluteUri);
        var body = new BodyBuilder
        {
            TextBody = $"Xin chào {displayName},\n\nChào mừng bạn đến với Culinary Blog. Hãy khám phá các công thức tại {siteUri.AbsoluteUri}",
            HtmlBody = $"""
                <!doctype html>
                <html lang="vi">
                <body>
                  <h1>Chào mừng đến với Culinary Blog</h1>
                  <p>Xin chào {safeDisplayName},</p>
                  <p>Cảm ơn bạn đã tham gia cộng đồng. Hãy khám phá các công thức mới.</p>
                  <p><a href="{siteUrl}">Mở Culinary Blog</a></p>
                </body>
                </html>
                """,
        };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            port,
            security,
            cancellationToken);

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
}
