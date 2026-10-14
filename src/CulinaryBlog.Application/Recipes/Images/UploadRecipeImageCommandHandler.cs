using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Recipes;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class UploadRecipeImageCommandHandler(
    IRecipeRepository recipeRepository,
    IRecipeAuthorizationService recipeAuthorization,
    IFileStorageService fileStorage,
    IRecipeImageThumbnailPublisher thumbnailPublisher,
    TimeProvider timeProvider,
    ILogger<UploadRecipeImageCommandHandler> logger)
    : ICommandHandler<UploadRecipeImageCommand, Guid>
{
    private static readonly Action<ILogger, Guid, Exception?> LogCompensationFailure =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(5102, "ImageUploadCompensationFailed"),
            "Upload compensation failed for recipe {RecipeId}; orphan file reconciliation is required.");

    public async Task<Guid> Handle(
        UploadRecipeImageCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.", "RECIPE_NOT_FOUND");

        recipeAuthorization.EnsureCanManage(recipe);
        recipeRepository.SetOriginalVersion(recipe, request.Version);

        var extension = await RecipeImageFileValidator.ValidateAsync(
            request.Content,
            request.ContentType,
            request.ContentLength,
            cancellationToken);
        var objectName = $"recipes/{recipe.Id}/{Guid.NewGuid():N}{extension}";
        var originalUrl = await fileStorage.UploadAsync(
            request.Content,
            objectName,
            request.ContentType,
            cancellationToken);

        RecipeImage image;
        try
        {
            image = recipe.AddImage(originalUrl, request.AltText);
            if (request.IsPrimary)
            {
                recipe.SetPrimaryImage(image.Id);
            }

            recipe.MarkUpdated(timeProvider.GetUtcNow());
            recipeRepository.AddImage(image);
            await recipeRepository.SaveImageChangesAsync(recipe, cancellationToken);
        }
        catch
        {
            try
            {
                // Compensation must still run when the HTTP request has been cancelled.
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await fileStorage.DeleteAsync(originalUrl, cleanupTimeout.Token);
            }
            catch (Exception exception) when (exception is FileStorageUnavailableException or OperationCanceledException)
            {
                LogCompensationFailure(logger, recipe.Id, exception);
            }

            throw;
        }

        thumbnailPublisher.Publish(new RecipeImageThumbnailRequested(
            recipe.Id,
            image.Id,
            image.OriginalUrl,
            $"thumbnail:recipe-image:{image.Id}"));

        return image.Id;
    }
}
