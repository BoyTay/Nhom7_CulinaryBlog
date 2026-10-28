namespace CulinaryBlog.Application.Auth.Commands.ForgotPassword;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

public sealed class ForgotPasswordCommandHandler(
    IIdentityService identityService,
    IPasswordResetEmailSender emailSender,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand>
{
    private static readonly Action<ILogger, Exception?> LogDeliveryFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(1301, "PasswordResetEmailFailed"),
            "Password reset email could not be delivered.");

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var ticket = await identityService.CreatePasswordResetTicketAsync(request.Email.Trim(), cancellationToken);
        if (ticket is null)
        {
            return;
        }

        try
        {
            await emailSender.SendAsync(ticket, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The response must not reveal whether the account exists, so delivery failures are only logged.
            LogDeliveryFailure(logger, exception);
        }
    }
}
