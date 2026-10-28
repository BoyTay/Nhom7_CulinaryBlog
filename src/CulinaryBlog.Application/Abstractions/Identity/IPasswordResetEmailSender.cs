namespace CulinaryBlog.Application.Abstractions.Identity;

public interface IPasswordResetEmailSender
{
    Task SendAsync(PasswordResetTicket ticket, CancellationToken cancellationToken = default);
}
