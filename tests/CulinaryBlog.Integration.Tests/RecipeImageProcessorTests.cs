using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeImageProcessorTests
{
    [Fact]
    public async Task ProcessorStoresBoundedWebpVariantsAndUpdatesTheRecipeAggregate()
    {
        var category = Category.Create("Soups", "soups");
        var recipe = Recipe.Create(
            "Noodle soup",
            "noodle-soup",
            "A warming soup.",
            category.Id,
            "author-1",
            10,
            20,
            2,
            RecipeDifficulty.Easy);
        var originalUrl = "https://storage.example/original.png";
        var image = recipe.AddImage(originalUrl);
        var storage = new TestFileStorage(CreatePngImage());
        var repository = new TestRecipeRepository(recipe);
        var processor = new RecipeImageProcessor(
            storage,
            repository,
            TimeProvider.System,
            NullLogger<RecipeImageProcessor>.Instance);

        await processor.ProcessAsync(recipe.Id, image.Id, originalUrl);

        Assert.True(repository.Saved);
        Assert.Equal(2, storage.Uploads.Count);
        Assert.Contains(storage.Uploads.Keys, key => key.EndsWith(".medium.webp", StringComparison.Ordinal));
        Assert.Contains(storage.Uploads.Keys, key => key.EndsWith(".thumbnail.webp", StringComparison.Ordinal));
        Assert.StartsWith("https://storage.example/", image.MediumUrl);
        Assert.StartsWith("https://storage.example/", image.ThumbnailUrl);

        using var medium = SKBitmap.Decode(new MemoryStream(storage.Uploads[image.MediumUrl!]));
        using var thumbnail = SKBitmap.Decode(new MemoryStream(storage.Uploads[image.ThumbnailUrl!]));
        Assert.NotNull(medium);
        Assert.NotNull(thumbnail);
        Assert.True(medium.Width <= 800 && medium.Height <= 600);
        Assert.True(thumbnail.Width <= 300 && thumbnail.Height <= 300);
    }

    private static byte[] CreatePngImage()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(1000, 800));
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private sealed class TestFileStorage(byte[] sourceBytes) : IFileStorageService
    {
        public Dictionary<string, byte[]> Uploads { get; } = new(StringComparer.Ordinal);

        public Task<string> UploadAsync(
            Stream content,
            string objectName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            using var output = new MemoryStream();
            content.CopyTo(output);
            var publicUrl = $"https://storage.example/{objectName}";
            Uploads.Add(publicUrl, output.ToArray());
            return Task.FromResult(publicUrl);
        }

        public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default)
        {
            Uploads.Remove(publicUrl);
            return Task.CompletedTask;
        }

        public Task<StoredFileInfo?> GetInfoAsync(
            string publicUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFileInfo?>(
                publicUrl == "https://storage.example/original.png"
                    ? new StoredFileInfo("original.png", "image/png", sourceBytes.Length, null)
                    : null);

        public Task<Stream?> OpenReadAsync(
            string publicUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(
                publicUrl == "https://storage.example/original.png"
                    ? new MemoryStream(sourceBytes, writable: false)
                    : null);
    }

    private sealed class TestRecipeRepository(Recipe recipe) : IRecipeRepository
    {
        public bool Saved { get; private set; }

        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(recipe.Id == id ? recipe : null);

        public Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(recipe.Slug == slug ? recipe : null);

        public Task<IReadOnlyList<Recipe>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>([]);

        public Task<IReadOnlyList<Recipe>> ListAsync(
            Guid? categoryId = null,
            RecipeDifficulty? difficulty = null,
            RecipeStatus? status = null,
            string? authorId = null,
            bool includeAllStatuses = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>([]);

        public void Add(Recipe recipe) => throw new NotSupportedException();

        public void AddStep(RecipeStep recipeStep) => throw new NotSupportedException();

        public void AddIngredient(RecipeIngredient recipeIngredient) => throw new NotSupportedException();

        public void AddImage(RecipeImage recipeImage) => throw new NotSupportedException();

        public Task SaveImageChangesAsync(Recipe updatedRecipe, CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.CompletedTask;
        }

        public void SetOriginalVersion(Recipe recipe, uint version) => throw new NotSupportedException();
    }
}
