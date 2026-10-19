using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class RecipeImageProcessor(
    IFileStorageService fileStorage,
    IRecipeRepository recipeRepository,
    TimeProvider timeProvider,
    ILogger<RecipeImageProcessor> logger) : IRecipeImageProcessor
{
    private const int MaximumSourceLength = 5 * 1024 * 1024;
    private const long MaximumDecodedPixels = 40_000_000;
    private static readonly Action<ILogger, Guid, Exception?> LogOrphanedImageVariants =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(9201, "ImageVariantCleanupFailed"),
            "Generated image variants could not be removed for recipe image {RecipeImageId}.");

    public async Task ProcessAsync(
        Guid recipeId,
        Guid recipeImageId,
        string originalUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalUrl);
        if (recipeId == Guid.Empty || recipeImageId == Guid.Empty)
        {
            throw new ArgumentException("Recipe and image IDs are required.");
        }

        var recipe = await recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
        {
            return;
        }

        var image = recipe.Images.SingleOrDefault(item => item.Id == recipeImageId);
        if (image is null || !string.Equals(image.OriginalUrl, originalUrl, StringComparison.Ordinal))
        {
            return;
        }

        var metadata = await fileStorage.GetInfoAsync(originalUrl, cancellationToken)
            ?? throw new FileNotFoundException("The source recipe image no longer exists.", originalUrl);
        if (metadata.ContentLength is <= 0 or > MaximumSourceLength)
        {
            throw new InvalidDataException(
                $"Source recipe image length must be between 1 and {MaximumSourceLength} bytes.");
        }

        await using var sourceStream = await fileStorage.OpenReadAsync(originalUrl, cancellationToken)
            ?? throw new FileNotFoundException("The source recipe image disappeared before it could be read.", originalUrl);
        using var codec = SKCodec.Create(sourceStream)
            ?? throw new InvalidDataException("The source recipe image could not be decoded.");
        var sourceInfo = codec.Info;

        if (sourceInfo.Width <= 0
            || sourceInfo.Height <= 0
            || (long)sourceInfo.Width * sourceInfo.Height > MaximumDecodedPixels)
        {
            throw new InvalidDataException("The source recipe image has invalid or excessive dimensions.");
        }

        using var sourceBitmap = SKBitmap.Decode(codec, sourceInfo)
            ?? throw new InvalidDataException("The source recipe image could not be decoded.");

        var objectPrefix = $"recipes/{recipe.Id}/variants/{recipeImageId:N}";
        var mediumUrl = await UploadVariantAsync(
            sourceBitmap,
            $"{objectPrefix}.medium.webp",
            800,
            600,
            cancellationToken);
        string thumbnailUrl;
        try
        {
            thumbnailUrl = await UploadVariantAsync(
                sourceBitmap,
                $"{objectPrefix}.thumbnail.webp",
                300,
                300,
                cancellationToken);
        }
        catch (Exception processingException)
        {
            await DeleteOrReportAsync(recipeImageId, [mediumUrl], processingException);
            throw;
        }

        try
        {
            recipe = await recipeRepository.GetByIdAsync(recipe.Id, cancellationToken);
            image = recipe?.Images.SingleOrDefault(item => item.Id == recipeImageId);
            if (recipe is null
                || image is null
                || !string.Equals(image.OriginalUrl, originalUrl, StringComparison.Ordinal))
            {
                await DeleteVariantsWithTimeoutAsync(recipeImageId, [mediumUrl, thumbnailUrl]);
                return;
            }

            recipe.UpdateImageVariants(recipeImageId, mediumUrl, thumbnailUrl);
            recipe.MarkUpdated(timeProvider.GetUtcNow());
            await recipeRepository.SaveImageChangesAsync(recipe, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await DeleteOrReportAsync(recipeImageId, [mediumUrl, thumbnailUrl], exception);
            throw;
        }
        catch (Exception exception)
        {
            await DeleteOrReportAsync(recipeImageId, [mediumUrl, thumbnailUrl], exception);
            throw;
        }
    }

    private async Task<string> UploadVariantAsync(
        SKBitmap source,
        string objectName,
        int maximumWidth,
        int maximumHeight,
        CancellationToken cancellationToken)
    {
        var ratio = Math.Min(
            1d,
            Math.Min(maximumWidth / (double)source.Width, maximumHeight / (double)source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var height = Math.Max(1, (int)Math.Round(source.Height * ratio));

        using var resized = source.Resize(
            new SKImageInfo(width, height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidDataException("The source recipe image could not be resized.");
        using var encoded = resized.Encode(SKEncodedImageFormat.Webp, 82)
            ?? throw new InvalidDataException("The resized recipe image could not be encoded.");
        await using var content = encoded.AsStream();
        return await fileStorage.UploadAsync(
            content,
            objectName,
            "image/webp",
            cancellationToken);
    }

    private async Task DeleteOrReportAsync(
        Guid recipeImageId,
        IReadOnlyCollection<string> urls,
        Exception processingException)
    {
        try
        {
            await DeleteVariantsWithTimeoutAsync(recipeImageId, urls);
        }
        catch (Exception cleanupException)
        {
            LogOrphanedImageVariants(logger, recipeImageId, cleanupException);
            throw new AggregateException(
                "Image processing failed and generated variants could not be cleaned up.",
                processingException,
                cleanupException);
        }
    }

    private async Task DeleteVariantsWithTimeoutAsync(Guid recipeImageId, IEnumerable<string> urls)
    {
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await DeleteVariantsAsync(recipeImageId, urls, cleanupTimeout.Token);
    }

    private async Task DeleteVariantsAsync(
        Guid recipeImageId,
        IEnumerable<string> urls,
        CancellationToken cancellationToken)
    {
        List<Exception>? failures = null;
        foreach (var url in urls.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await fileStorage.DeleteAsync(url, cancellationToken);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is not null)
        {
            throw new AggregateException(
                $"Could not clean up generated image variants for recipe image {recipeImageId}.",
                failures);
        }
    }

}
