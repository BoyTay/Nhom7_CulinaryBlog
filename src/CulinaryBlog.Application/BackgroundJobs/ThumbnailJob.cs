using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class ThumbnailJob(
    IRecipeImageProcessor processor,
    IJobExecutionStore executionStore)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        Guid recipeImageId,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        if (!await executionStore.TryClaimAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        await processor.ProcessAsync(recipeImageId, originalUrl, cancellationToken);
        await executionStore.MarkCompletedAsync(idempotencyKey, cancellationToken);
    }
}
