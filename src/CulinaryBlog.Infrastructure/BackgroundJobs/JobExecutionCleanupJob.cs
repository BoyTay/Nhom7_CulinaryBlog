using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class JobExecutionCleanupJob(
    IJobExecutionStore executionStore,
    TimeProvider timeProvider,
    HangfireOptions? options = null)
{

    public Task ExecuteAsync(CancellationToken cancellationToken = default) =>
        executionStore.DeleteExpiredBeforeAsync(
            timeProvider.GetUtcNow().AddDays(-(options?.RetentionDays ?? 90)),
            cancellationToken);
}
