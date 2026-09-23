using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class SitemapJob(
    ISitemapWriter writer,
    IJobExecutionStore executionStore)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!await executionStore.TryClaimAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        await writer.GenerateAsync(cancellationToken);
        await executionStore.MarkCompletedAsync(idempotencyKey, cancellationToken);
    }
}
