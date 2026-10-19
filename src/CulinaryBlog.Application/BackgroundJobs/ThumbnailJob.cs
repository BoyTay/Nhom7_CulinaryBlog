using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public sealed class ThumbnailJob(
    IRecipeImageProcessor processor,
    IJobExecutionStore executionStore,
    JobExecutionOptions? options = null)
{
    public async Task ExecuteAsync(
        string idempotencyKey,
        Guid recipeId,
        Guid recipeImageId,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        await IdempotentJobExecution.RunAsync(
            executionStore,
            idempotencyKey,
            token => processor.ProcessAsync(recipeId, recipeImageId, originalUrl, token),
            timeout: options?.Timeout,
            cancellationToken: cancellationToken);
    }
}
