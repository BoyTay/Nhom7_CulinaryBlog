using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class RecipeImageThumbnailPublisher(IBackgroundJobScheduler jobScheduler)
    : IRecipeImageThumbnailPublisher
{
    public string Publish(RecipeImageThumbnailRequested request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return jobScheduler.Enqueue<ThumbnailJob>(job => job.ExecuteAsync(
            request.IdempotencyKey,
            request.RecipeId,
            request.RecipeImageId,
            request.OriginalUrl,
            CancellationToken.None));
    }
}
