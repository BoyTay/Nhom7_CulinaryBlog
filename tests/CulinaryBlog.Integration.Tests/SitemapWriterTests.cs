using System.Xml.Linq;
using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Integration.Tests;

public sealed class SitemapWriterTests
{
    [Fact]
    public async Task WriterUploadsCanonicalXmlForStaticPublishedRecipeAndCategoryPages()
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
        recipe.AddIngredient("Noodles", 1, "cup");
        recipe.AddStep("Boil the noodles.");
        recipe.Publish();
        var draft = Recipe.Create(
            "Draft soup",
            "draft-soup",
            "Not yet published.",
            category.Id,
            "author-1",
            10,
            20,
            2,
            RecipeDifficulty.Easy);

        var storage = new RecordingFileStorage();
        var writer = new SitemapWriter(
            new TestRecipeRepository(recipe, draft),
            new TestCategoryRepository(category),
            storage,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicSite:BaseUrl"] = "https://food.example/blog/",
            }).Build(),
            NullLogger<SitemapWriter>.Instance);

        await writer.GenerateAsync();

        Assert.Equal("sitemap.xml", storage.ObjectName);
        Assert.Equal("application/xml; charset=utf-8", storage.ContentType);
        var document = XDocument.Load(new MemoryStream(storage.Content!));
        XNamespace sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlElements = document.Root!.Elements(sitemap + "url").ToArray();
        var locations = urlElements.Select(element => element.Element(sitemap + "loc")!.Value).ToArray();

        Assert.Equal(5, locations.Length);
        Assert.Contains("https://food.example/blog/", locations);
        Assert.Contains("https://food.example/blog/recipes", locations);
        Assert.Contains("https://food.example/blog/search", locations);
        Assert.Contains($"https://food.example/blog/recipes/{recipe.Id:D}", locations);
        Assert.Contains($"https://food.example/blog/recipes?categoryId={category.Id:D}", locations);
        Assert.DoesNotContain(locations, location => location.Contains("draft-soup", StringComparison.Ordinal));
        Assert.All(locations, location => Assert.StartsWith("https://food.example/blog", location, StringComparison.Ordinal));
        Assert.All(urlElements, element =>
        {
            Assert.NotNull(element.Element(sitemap + "changefreq"));
            Assert.NotNull(element.Element(sitemap + "priority"));
        });
    }

    private sealed class RecordingFileStorage : IFileStorageService
    {
        public string? ObjectName { get; private set; }

        public string? ContentType { get; private set; }

        public byte[]? Content { get; private set; }

        public async Task<string> UploadAsync(
            Stream content,
            string objectName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ObjectName = objectName;
            ContentType = contentType;
            await using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            Content = output.ToArray();
            return "https://storage.example/sitemap.xml";
        }

        public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StoredFileInfo?> GetInfoAsync(string publicUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFileInfo?>(null);

        public Task<Stream?> OpenReadAsync(string publicUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);
    }

    private sealed class TestRecipeRepository(params Recipe[] recipes) : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(recipes.SingleOrDefault(recipe => recipe.Id == id));

        public Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(recipes.SingleOrDefault(recipe => recipe.Slug == slug));

        public Task<IReadOnlyList<Recipe>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>(
                recipes.Where(recipe => recipe.Status == RecipeStatus.Published).ToArray());

        public Task<IReadOnlyList<Recipe>> ListAsync(
            Guid? categoryId = null,
            RecipeDifficulty? difficulty = null,
            RecipeStatus? status = null,
            string? authorId = null,
            bool includeAllStatuses = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>(recipes);

        public void Add(Recipe recipe) => throw new NotSupportedException();

        public void AddStep(RecipeStep recipeStep) => throw new NotSupportedException();

        public void AddIngredient(RecipeIngredient recipeIngredient) => throw new NotSupportedException();

        public void AddImage(RecipeImage recipeImage) => throw new NotSupportedException();

        public Task SaveImageChangesAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void SetOriginalVersion(Recipe recipe, uint version) => throw new NotSupportedException();
    }

    private sealed class TestCategoryRepository(Category category) : ICategoryRepository
    {
        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Category?>(category.Id == id ? category : null);

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<Category?>(category.Slug == slug ? category : null);

        public Task<bool> NameExistsAsync(
            string name,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> CountRecipesAsync(
            Guid categoryId,
            bool publishedOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
            Guid categoryId,
            string? authorId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CategoryRecipeSummary>, int)>(([], 0));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>([category]);

        public void Add(Category category) => throw new NotSupportedException();
    }
}
