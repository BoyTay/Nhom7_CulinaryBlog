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
        await IdempotentJobExecution.RunAsync(
            executionStore,
            idempotencyKey,
            token => sender.SendAsync(email, displayName, token),
            cancellationToken);
    }
}
