using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class SitemapJob(
    ISitemapWriter writer,
    IJobExecutionStore executionStore,
    TimeProvider timeProvider,
    JobExecutionOptions? options = null)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await IdempotentJobExecution.RunAsync(
            executionStore,
            idempotencyKey,
            writer.GenerateAsync,
            timeout: options?.Timeout,
            cancellationToken: cancellationToken);
    }

    public Task ExecuteRecurringAsync(CancellationToken cancellationToken = default)
    {
        var utcDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return ExecuteAsync($"sitemap:{utcDate:yyyy-MM-dd}", cancellationToken);
    }
}
