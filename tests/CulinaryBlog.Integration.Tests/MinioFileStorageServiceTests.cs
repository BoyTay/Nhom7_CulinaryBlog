using System.Net;
using CulinaryBlog.Application.Abstractions.FileStorage;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Integration.Tests;

public sealed class MinioFileStorageServiceTests
{
    [MinioFact]
    public async Task AdapterUploadsPublicImageAndDeletesItIdempotently()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = new HttpClient();
        var fileStorage = factory.Services.GetRequiredService<IFileStorageService>();
        var objectName = $"recipes/integration-tests/{Guid.NewGuid():N}.png";
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        await using var content = new MemoryStream(imageBytes);
        content.Position = 3;
        var publicUrl = await fileStorage.UploadAsync(content, objectName, "image/png");
        try
        {
            using var uploaded = await client.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
            Assert.Equal("image/png", uploaded.Content.Headers.ContentType?.MediaType);
            Assert.Equal(imageBytes, await uploaded.Content.ReadAsByteArrayAsync());
            var metadata = await fileStorage.GetInfoAsync(publicUrl);
            Assert.NotNull(metadata);
            Assert.Equal(objectName, metadata.ObjectName);
            Assert.Equal("image/png", metadata.ContentType);
            Assert.Equal(imageBytes.Length, metadata.ContentLength);
            Assert.False(string.IsNullOrWhiteSpace(metadata.ETag));

            await fileStorage.DeleteAsync(publicUrl);
            await fileStorage.DeleteAsync(publicUrl);

            using var deleted = await client.GetAsync(publicUrl);
            Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
            Assert.Null(await fileStorage.GetInfoAsync(publicUrl));
        }
        finally
        {
            await fileStorage.DeleteAsync(publicUrl);
        }
    }

    [MinioFact]
    public async Task AdapterRejectsForeignUrlsAndTraversalKeys()
    {
        await using var factory = new TestWebApplicationFactory();
        var fileStorage = factory.Services.GetRequiredService<IFileStorageService>();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            fileStorage.DeleteAsync("https://foreign.example.test/culinary-blog/photo.png"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            fileStorage.GetInfoAsync("https://foreign.example.test/culinary-blog/photo.png"));
        using var content = new MemoryStream(new byte[8]);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            fileStorage.UploadAsync(content, "recipes/../photo.png", "image/png"));
    }
}

public sealed class MinioFactAttribute : FactAttribute
{
    public MinioFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MINIO_INTEGRATION_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set MINIO_INTEGRATION_TESTS=true when a local MinIO service is available.";
        }
    }
}
