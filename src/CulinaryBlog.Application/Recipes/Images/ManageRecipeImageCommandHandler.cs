using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class ManageRecipeImageCommandHandler(
    IRecipeRepository recipeRepository,
    IRecipeAuthorizationService recipeAuthorization,
    IFileStorageService fileStorage,
    IBackgroundJobScheduler jobScheduler,
    TimeProvider timeProvider,
    ILogger<ManageRecipeImageCommandHandler> logger)
    : ICommandHandler<ManageRecipeImageCommand, Guid>
{
    private static readonly Action<ILogger, Guid, Exception?> LogCleanupSchedulingFailure =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(5101, "ImageCleanupSchedulingFailed"),
            "Could not schedule file cleanup for recipe image {ImageId}; attempting direct deletion.");

    public async Task<Guid> Handle(
        ManageRecipeImageCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.", "RECIPE_NOT_FOUND");

        recipeAuthorization.EnsureCanManage(recipe);
        var image = recipe.Images.SingleOrDefault(item => item.Id == request.ImageId)
            ?? throw new NotFoundException("Recipe image not found.", "IMAGE_NOT_FOUND");

        recipeRepository.SetOriginalVersion(recipe, request.Version);
        recipe.MarkUpdated(timeProvider.GetUtcNow());

        var deletedUrls = request.Action switch
        {
            RecipeImageAction.SetPrimary => SetPrimary(recipe, request.ImageId),
            RecipeImageAction.Delete => DeleteImage(recipe, image),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unsupported recipe image action."),
        };

        await recipeRepository.SaveImageChangesAsync(recipe, cancellationToken);

        // The DB commit is complete. A disconnected client must not cancel cleanup.
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Exception? cleanupFailure = null;
        foreach (var deletedUrl in deletedUrls)
        {
            string? jobId = null;
            try
            {
                jobId = jobScheduler.Enqueue<DeleteRecipeImageFileJob>(
                    job => job.ExecuteAsync(deletedUrl, CancellationToken.None));
            }
            catch (Exception exception)
            {
                LogCleanupSchedulingFailure(logger, request.ImageId, exception);
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                try
                {
                    await fileStorage.DeleteAsync(deletedUrl, cleanupTimeout.Token);
                }
                catch (Exception exception) when (exception is FileStorageUnavailableException or OperationCanceledException)
                {
                    // Attempt all variants even when an earlier object cannot be removed.
                    cleanupFailure ??= exception;
                }
            }
        }

        if (cleanupFailure is not null)
        {
            throw new FileStorageUnavailableException("The image was removed but file cleanup could not finish.", cleanupFailure);
        }

        return request.ImageId;
    }

    private static string[] SetPrimary(Recipe recipe, Guid imageId)
    {
        recipe.SetPrimaryImage(imageId);
        return [];
    }

    private static string[] DeleteImage(Recipe recipe, RecipeImage image)
    {
        var urls = new[] { image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl }
            .OfType<string>()
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        recipe.RemoveImage(image.Id);
        return urls;
    }
}
