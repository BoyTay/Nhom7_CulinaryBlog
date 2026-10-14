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
        await IdempotentJobExecution.RunAsync(
            executionStore,
            idempotencyKey,
            token => processor.ProcessAsync(recipeImageId, originalUrl, token),
            cancellationToken: cancellationToken);
    }
}
