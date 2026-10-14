using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class JobExecutionCleanupJob(
    IJobExecutionStore executionStore,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    public Task ExecuteAsync(CancellationToken cancellationToken = default) =>
        executionStore.DeleteExpiredBeforeAsync(
            timeProvider.GetUtcNow().Subtract(RetentionPeriod),
            cancellationToken);
}
