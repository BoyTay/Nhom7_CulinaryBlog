using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Images;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Categories;

public sealed record UploadCategoryImageCommand(Guid Id, Stream Content, long ContentLength, string ContentType)
    : ICommand<CategoryDto>;

public sealed class UploadCategoryImageCommandValidator : AbstractValidator<UploadCategoryImageCommand>
{
    public UploadCategoryImageCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class UploadCategoryImageCommandHandler(
    ICategoryRepository categories,
    IDataSession session,
    IMemoryCache cache,
    IFileStorageService fileStorage,
    ILogger<UploadCategoryImageCommandHandler> logger)
    : ICommandHandler<UploadCategoryImageCommand, CategoryDto>
{
    private static readonly Action<ILogger, Exception?> LogCleanupFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1303, "CategoryImageCleanupFailed"),
            "A category image file could not be deleted; orphan file reconciliation is required.");

    public async Task<CategoryDto> Handle(UploadCategoryImageCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.", "CATEGORY_NOT_FOUND");

        // Category images accept the same formats, size limit and signature checks as recipe images.
        var extension = await RecipeImageFileValidator.ValidateAsync(
            request.Content,
            request.ContentType,
            request.ContentLength,
            cancellationToken);
        var prefix = $"categories/{category.Id}/";
        var imageUrl = await fileStorage.UploadAsync(
            request.Content,
            $"{prefix}{Guid.NewGuid():N}{extension}",
            request.ContentType,
            cancellationToken);
        var previousUrl = category.ImageUrl;

        try
        {
            category.Update(category.Name, category.Description, imageUrl, category.OrderIndex);
            await session.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(imageUrl);
            throw;
        }

        cache.Remove(CategoryQueries.CacheKey);
        // Only files this feature stored are removed; a pasted external URL is never touched.
        if (previousUrl is not null && previousUrl.Contains($"/{prefix}", StringComparison.Ordinal))
        {
            await TryDeleteAsync(previousUrl);
        }

        var recipeCount = await categories.CountRecipesAsync(category.Id, true, cancellationToken);
        return CategoryDto.From(category, recipeCount);
    }

    private async Task TryDeleteAsync(string url)
    {
        try
        {
            // Cleanup must still run when the HTTP request has been cancelled.
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await fileStorage.DeleteAsync(url, cleanupTimeout.Token);
        }
        catch (Exception exception) when (exception is FileStorageUnavailableException or OperationCanceledException or ArgumentException)
        {
            LogCleanupFailure(logger, exception);
        }
    }
}
