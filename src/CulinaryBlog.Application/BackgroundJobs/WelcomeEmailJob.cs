using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class WelcomeEmailJob(
    IWelcomeEmailSender sender,
    IJobExecutionStore executionStore)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (!await executionStore.TryClaimAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        await sender.SendAsync(email, displayName, cancellationToken);
        await executionStore.MarkCompletedAsync(idempotencyKey, cancellationToken);
    }
}
