using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Images;
using CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeImageEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00];
    private readonly HttpClient _client;
    private readonly FakeFileStorageService _fileStorage;
    private readonly CapturingThumbnailPublisher _thumbnailPublisher;
    private readonly WebApplicationFactory<Program> _imageFactory;
    private readonly NoOpScheduler _jobScheduler = new();

    public RecipeImageEndpointTests(TestWebApplicationFactory factory)
    {
        _fileStorage = new FakeFileStorageService();
        _thumbnailPublisher = new CapturingThumbnailPublisher();
        _imageFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService>(_fileStorage);
            services.RemoveAll<IRecipeImageThumbnailPublisher>();
            services.AddSingleton<IRecipeImageThumbnailPublisher>(_thumbnailPublisher);
            services.RemoveAll<IBackgroundJobScheduler>();
            services.AddSingleton<IBackgroundJobScheduler>(_jobScheduler);
        }));
        _client = _imageFactory.CreateClient();
    }

    [Fact]
    public async Task UploadStoresImageAndPublishesStableThumbnailRequest()
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);

        using var request = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png", true);
        using var response = await _client.SendAsync(request);
        var image = await response.Content.ReadFromJsonAsync<RecipeImageDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(image);
        Assert.Equal(image.OriginalUrl, image.Url);
        Assert.True(image.IsPrimary);
        Assert.Equal(0, image.OrderIndex);
        Assert.NotEqual(GetEtag(original.Response), GetEtag(response));
        Assert.StartsWith($"recipes/{recipeId}/", _fileStorage.LastObjectName);
        Assert.EndsWith(".png", _fileStorage.LastObjectName);
        Assert.NotNull(_thumbnailPublisher.LastRequest);
        Assert.Equal(recipeId, _thumbnailPublisher.LastRequest.RecipeId);
        Assert.Equal(image.Id, _thumbnailPublisher.LastRequest.RecipeImageId);
        Assert.Equal(image.OriginalUrl, _thumbnailPublisher.LastRequest.OriginalUrl);
        Assert.Equal($"thumbnail:recipe-image:{image.Id}", _thumbnailPublisher.LastRequest.IdempotencyKey);
    }

    [Fact]
    public async Task UploadRejectsMismatchedMagicBytesWithoutCallingStorage()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);
        var uploadCount = _fileStorage.UploadCount;

        using var request = CreateUploadRequest(recipeId, recipe.Response, new byte[16], "image/png");
        using var response = await _client.SendAsync(request);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("FILE_SIGNATURE_INVALID", problem.RootElement.GetProperty("type").GetString());
        Assert.Equal(uploadCount, _fileStorage.UploadCount);
    }

    [Fact]
    public async Task StorageFailureReturnsServiceUnavailable()
    {
        var recipeId = await CreateRecipeAsync();
        var recipe = await GetRecipeAsync(recipeId);
        _fileStorage.ThrowOnUpload = true;

        using var request = CreateUploadRequest(recipeId, recipe.Response, PngSignature, "image/png");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task OnlyOwnerOrAdminCanUploadAndImageLifecyclePreservesSinglePrimary()
    {
        var recipeId = await CreateRecipeAsync();
        var initial = await GetRecipeAsync(recipeId);
        using var nonOwnerRequest = CreateUploadRequest(recipeId, initial.Response, PngSignature, "image/png");
        nonOwnerRequest.Headers.Add("X-Test-User", "another-author");
        using var forbidden = await _client.SendAsync(nonOwnerRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var adminRequest = CreateUploadRequest(recipeId, initial.Response, PngSignature, "image/png", true);
        adminRequest.Headers.Add("X-Test-User", "site-admin");
        adminRequest.Headers.Add("X-Test-Role", "Admin");
        using var createdByAdmin = await _client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.Created, createdByAdmin.StatusCode);
        var adminImage = await createdByAdmin.Content.ReadFromJsonAsync<RecipeImageDto>();
        Assert.NotNull(adminImage);

        var afterAdminUpload = await GetRecipeAsync(recipeId);
        using var anotherUploadRequest = CreateUploadRequest(
            recipeId,
            afterAdminUpload.Response,
            PngSignature,
            "image/png");
        using var anotherCreated = await _client.SendAsync(anotherUploadRequest);
        Assert.Equal(HttpStatusCode.Created, anotherCreated.StatusCode);
        var anotherImage = await anotherCreated.Content.ReadFromJsonAsync<RecipeImageDto>();
        Assert.NotNull(anotherImage);
        Assert.False(anotherImage.IsPrimary);

        var latest = await GetRecipeAsync(recipeId);
        using var setPrimary = CreateMutation(
            HttpMethod.Patch,
            $"/api/v1/recipes/{recipeId}/images/{anotherImage.Id}/primary",
            GetEtag(latest.Response));
        using var primaryResponse = await _client.SendAsync(setPrimary);
        Assert.Equal(HttpStatusCode.OK, primaryResponse.StatusCode);

        var withPrimary = await GetRecipeAsync(recipeId);
        Assert.Single(withPrimary.Detail.Images, image => image.IsPrimary);
        Assert.True(withPrimary.Detail.Images.Single(image => image.Id == anotherImage.Id).IsPrimary);

        using var deletePrimary = CreateMutation(
            HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/images/{anotherImage.Id}",
            GetEtag(withPrimary.Response));
        using var deleted = await _client.SendAsync(deletePrimary);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Contains(anotherImage.OriginalUrl, _fileStorage.DeletedUrls);

        var afterDelete = await GetRecipeAsync(recipeId);
        Assert.Single(afterDelete.Detail.Images, image => image.IsPrimary);
        Assert.True(afterDelete.Detail.Images.Single().IsPrimary);
        Assert.Equal(0, afterDelete.Detail.Images.Single().OrderIndex);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task ConcurrentUploadsWithSameEtagKeepOneImageAndCleanUpLoser(int requestCount)
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        var requests = Enumerable.Range(0, requestCount)
            .Select(_ => CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png"))
            .ToArray();
        var responses = await Task.WhenAll(requests.Select(request => _client.SendAsync(request)));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            var conflicts = responses.Where(response => response.StatusCode == HttpStatusCode.UnprocessableEntity).ToArray();
            Assert.Equal(requestCount - 1, conflicts.Length);
            foreach (var conflict in conflicts)
            {
                using var problem = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
                Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", problem.RootElement.GetProperty("type").GetString());
            }
            var latest = await GetRecipeAsync(recipeId);
            Assert.True(Assert.Single(latest.Detail.Images).IsPrimary);
            Assert.Equal(requestCount - 1, _fileStorage.DeletedUrls.Length);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
            foreach (var request in requests) request.Dispose();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DeleteLastImageAttemptsAllVariantsAndAllowsAnotherUpload(bool schedulerUnavailable, bool storageUnavailable)
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        using var upload = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
        using var created = await _client.SendAsync(upload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var image = await created.Content.ReadFromJsonAsync<RecipeImageDto>();
        Assert.NotNull(image);
        var mediumUrl = image.OriginalUrl + "-medium";
        var thumbnailUrl = image.OriginalUrl + "-thumbnail";
        using (var scope = _imageFactory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<CulinaryBlog.Application.Abstractions.Persistence.IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(recipeId);
            Assert.NotNull(recipe);
            recipe.UpdateImageVariants(image.Id, mediumUrl, thumbnailUrl);
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();
        }

        var current = await GetRecipeAsync(recipeId);
        _jobScheduler.ThrowOnEnqueue = schedulerUnavailable;
        _fileStorage.FailDeleteUrl = storageUnavailable ? image.OriginalUrl : null;
        using var delete = CreateMutation(HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/images/{image.Id}", GetEtag(current.Response));
        using var deleted = await _client.SendAsync(delete);
        Assert.Equal(storageUnavailable ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Contains(image.OriginalUrl, _fileStorage.DeletedUrls);
        Assert.Contains(mediumUrl, _fileStorage.DeletedUrls);
        Assert.Contains(thumbnailUrl, _fileStorage.DeletedUrls);
        var empty = await GetRecipeAsync(recipeId);
        Assert.Empty(empty.Detail.Images);
        using var replacement = CreateUploadRequest(recipeId, empty.Response, PngSignature, "image/png");
        using var replaced = await _client.SendAsync(replacement);
        Assert.Equal(HttpStatusCode.Created, replaced.StatusCode);
        var latest = await GetRecipeAsync(recipeId);
        Assert.True(Assert.Single(latest.Detail.Images).IsPrimary);
    }

    [Theory]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task NonOwnerCannotChangeOrDeleteImage(string method)
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        using var upload = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
        using var created = await _client.SendAsync(upload);
        var image = await created.Content.ReadFromJsonAsync<RecipeImageDto>();
        Assert.NotNull(image);
        var suffix = method == "PATCH" ? "/primary" : string.Empty;
        using var request = CreateMutation(new HttpMethod(method),
            $"/api/v1/recipes/{recipeId}/images/{image.Id}{suffix}", GetEtag(created));
        request.Headers.Add("X-Test-User", "another-author");
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(_fileStorage.DeletedUrls);
    }

    [Fact]
    public async Task UploadRequiresEtag()
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        using var request = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
        request.Headers.Remove("If-Match");
        using var response = await _client.SendAsync(request);
        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        Assert.Equal(0, _fileStorage.UploadCount);
    }

    [Theory]
    [InlineData("image/gif", 12, "FILE_MIME_INVALID")]
    [InlineData("image/png", 0, "FILE_EMPTY")]
    [InlineData("image/png", 5242881, "FILE_SIZE_EXCEEDED")]
    public async Task InvalidUploadReturnsDocumentedFileError(string mimeType, int size, string code)
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        using var request = CreateUploadRequest(recipeId, original.Response, new byte[size], mimeType);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, problem.RootElement.GetProperty("type").GetString());
        Assert.Equal(0, _fileStorage.UploadCount);
    }

    [Fact]
    public async Task UploadWithoutAuthenticationReturnsUnauthorized()
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        await using var anonymousFactory = _imageFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            })));
        using var client = anonymousFactory.CreateClient();
        using var request = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _fileStorage.UploadCount);
    }

    [Theory]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task StaleImageMutationDoesNotChangeImages(string method)
    {
        var recipeId = await CreateRecipeAsync();
        var original = await GetRecipeAsync(recipeId);
        using var upload = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
        using var created = await _client.SendAsync(upload);
        var image = await created.Content.ReadFromJsonAsync<RecipeImageDto>();
        Assert.NotNull(image);
        var suffix = method == "PATCH" ? "/primary" : string.Empty;
        using var request = CreateMutation(new HttpMethod(method),
            $"/api/v1/recipes/{recipeId}/images/{image.Id}{suffix}", GetEtag(original.Response));
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var latest = await GetRecipeAsync(recipeId);
        Assert.True(Assert.Single(latest.Detail.Images).IsPrimary);
        Assert.Empty(_fileStorage.DeletedUrls);
    }

    [Fact]
    public async Task DeletingFirstOfManyImagesReordersWithoutUniqueIndexConflict()
    {
        var recipeId = await CreateRecipeAsync();
        for (var index = 0; index < 5; index++)
        {
            var original = await GetRecipeAsync(recipeId);
            using var upload = CreateUploadRequest(recipeId, original.Response, PngSignature, "image/png");
            using var created = await _client.SendAsync(upload);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        var before = await GetRecipeAsync(recipeId);
        var imageId = before.Detail.Images[0].Id;
        using var delete = CreateMutation(HttpMethod.Delete,
            $"/api/v1/recipes/{recipeId}/images/{imageId}", GetEtag(before.Response));
        using var response = await _client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await GetRecipeAsync(recipeId);
        Assert.Equal([0, 1, 2, 3], after.Detail.Images.Select(image => image.OrderIndex));
        Assert.True(after.Detail.Images[0].IsPrimary);
        Assert.Single(after.Detail.Images, image => image.IsPrimary);
    }

    private async Task<Guid> CreateRecipeAsync()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeRequest(
                "File test recipe",
                $"file-test-{Guid.NewGuid():N}",
                "Recipe for image endpoint tests.",
                Guid.NewGuid(),
                5,
                15,
                2,
                RecipeDifficulty.Easy));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<(HttpResponseMessage Response, RecipeDetailDto Detail)> GetRecipeAsync(Guid recipeId)
    {
        var response = await _client.GetAsync($"/api/v1/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(detail);
        return (response, detail);
    }

    private static HttpRequestMessage CreateUploadRequest(
        Guid recipeId,
        HttpResponseMessage previous,
        byte[] bytes,
        string contentType,
        bool isPrimary = false)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(file, "file", "recipe-image.png");
        content.Add(new StringContent("Recipe photo"), "altText");
        content.Add(new StringContent(isPrimary.ToString()), "isPrimary");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{recipeId}/images")
        {
            Content = content,
        };
        request.Headers.TryAddWithoutValidation("If-Match", GetEtag(previous));
        return request;
    }

    private static HttpRequestMessage CreateMutation(HttpMethod method, string url, string etag)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static string GetEtag(HttpResponseMessage response) => response.Headers.GetValues("ETag").Single();

    private sealed class FakeFileStorageService : IFileStorageService
    {
        private readonly ConcurrentQueue<string> _deletedUrls = new();
        private int _uploadCount;

        public string LastObjectName { get; private set; } = string.Empty;

        public int UploadCount => Volatile.Read(ref _uploadCount);

        public bool ThrowOnUpload { get; set; }
        public string? FailDeleteUrl { get; set; }

        public string[] DeletedUrls => _deletedUrls.ToArray();

        public Task<string> UploadAsync(
            Stream content,
            string objectName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnUpload)
            {
                throw new FileStorageUnavailableException("Test storage failure.");
            }

            LastObjectName = objectName;
            Interlocked.Increment(ref _uploadCount);
            return Task.FromResult($"https://images.example.test/culinary-blog/{objectName}");
        }

        public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default)
        {
            _deletedUrls.Enqueue(publicUrl);
            if (publicUrl == FailDeleteUrl)
            {
                throw new FileStorageUnavailableException("Test delete failure.");
            }
            return Task.CompletedTask;
        }

        public Task<StoredFileInfo?> GetInfoAsync(string publicUrl, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Metadata is verified against the MinIO adapter.");
    }

    private sealed class CapturingThumbnailPublisher : IRecipeImageThumbnailPublisher
    {
        public RecipeImageThumbnailRequested? LastRequest { get; private set; }

        public string Publish(RecipeImageThumbnailRequested request)
        {
            LastRequest = request;
            return "test-thumbnail-job";
        }
    }

    private sealed class NoOpScheduler : IBackgroundJobScheduler
    {
        public bool ThrowOnEnqueue { get; set; }

        public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall) =>
            ThrowOnEnqueue ? throw new InvalidOperationException("Test scheduler failure.") : string.Empty;

        public void AddOrUpdate<TJob>(
            string recurringJobId,
            Expression<Func<TJob, Task>> methodCall,
            string cronExpression)
        {
        }
    }
}
