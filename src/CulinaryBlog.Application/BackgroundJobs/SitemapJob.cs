using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class SitemapJob(
    ISitemapWriter writer,
    IJobExecutionStore executionStore,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await IdempotentJobExecution.RunAsync(
            executionStore,
            idempotencyKey,
            writer.GenerateAsync,
            cancellationToken: cancellationToken);
    }

    public Task ExecuteRecurringAsync(CancellationToken cancellationToken = default)
    {
        var utcDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return ExecuteAsync($"sitemap:{utcDate:yyyy-MM-dd}", cancellationToken);
    }
}
