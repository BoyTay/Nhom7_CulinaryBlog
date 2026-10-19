using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class UserRegisteredWelcomeEmailHandler(
    IBackgroundJobScheduler scheduler,
    ILogger<UserRegisteredWelcomeEmailHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    private static readonly Action<ILogger, string, Exception?> LogWelcomeEmailNotScheduled =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(9101, "WelcomeEmailNotScheduled"),
            "Welcome email for user {UserId} was not scheduled because Hangfire is disabled.");

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var jobId = scheduler.Enqueue<WelcomeEmailJob>(
            job => job.ExecuteAsync(
                $"welcome-email:user:{notification.UserId}",
                notification.Email,
                notification.DisplayName,
                CancellationToken.None),
            "critical");

        if (string.IsNullOrWhiteSpace(jobId))
        {
            LogWelcomeEmailNotScheduled(logger, notification.UserId, null);
        }

        return Task.CompletedTask;
    }
}
