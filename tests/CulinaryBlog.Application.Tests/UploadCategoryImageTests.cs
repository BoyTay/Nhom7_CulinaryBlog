using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Categories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Application.Tests;

public sealed class UploadCategoryImageTests
{
    private const string StorageRoot = "http://storage.test/culinary-blog/";
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    [Fact]
    public async Task StoresTheImageUnderTheCategoryAndSavesItsUrl()
    {
        var (handler, repository, storage, session, category) = Create();
        using var content = new MemoryStream(PngHeader);

        var result = await handler.Handle(new UploadCategoryImageCommand(category.Id, content, content.Length, "image/png"), default);

        var uploaded = Assert.Single(storage.Uploaded);
        Assert.StartsWith($"categories/{category.Id}/", uploaded);
        Assert.EndsWith(".png", uploaded);
        Assert.Equal(StorageRoot + uploaded, result.ImageUrl);
        Assert.Equal(StorageRoot + uploaded, category.ImageUrl);
        Assert.Equal(1, session.SaveCount);
        Assert.Equal("Món chính", result.Name);
        Assert.NotNull(repository);
    }

    [Fact]
    public async Task ReplacingAnImageDeletesThePreviousStoredFileButNeverAnExternalUrl()
    {
        var (handler, _, storage, _, category) = Create();
        category.Update("Món chính", null, $"{StorageRoot}categories/{category.Id}/old.png", 0);
        using var first = new MemoryStream(PngHeader);
        await handler.Handle(new UploadCategoryImageCommand(category.Id, first, first.Length, "image/png"), default);
        Assert.Equal([$"{StorageRoot}categories/{category.Id}/old.png"], storage.Deleted);

        storage.Deleted.Clear();
        category.Update("Món chính", null, "https://example.test/pasted-link.jpg", 0);
        using var second = new MemoryStream(PngHeader);
        await handler.Handle(new UploadCategoryImageCommand(category.Id, second, second.Length, "image/png"), default);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task UnknownCategoryIsNotFoundAndNothingIsUploaded()
    {
        var (handler, _, storage, _, _) = Create();
        using var content = new MemoryStream(PngHeader);

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UploadCategoryImageCommand(Guid.NewGuid(), content, content.Length, "image/png"), default));

        Assert.Equal("CATEGORY_NOT_FOUND", error.Code);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task FilesThatAreNotImagesAreRejectedBeforeUploading()
    {
        var (handler, _, storage, session, category) = Create();
        using var content = new MemoryStream([0x4D, 0x5A, 0x90, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            handler.Handle(new UploadCategoryImageCommand(category.Id, content, content.Length, "image/png"), default));

        Assert.Empty(storage.Uploaded);
        Assert.Equal(0, session.SaveCount);
        Assert.Null(category.ImageUrl);
    }

    [Fact]
    public async Task ARejectedSaveRemovesTheFileThatWasJustUploaded()
    {
        var (handler, _, storage, session, category) = Create();
        session.FailNextSave = true;
        using var content = new MemoryStream(PngHeader);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UploadCategoryImageCommand(category.Id, content, content.Length, "image/png"), default));

        Assert.Equal(storage.Uploaded.Select(name => StorageRoot + name), storage.Deleted);
    }

    private static (UploadCategoryImageCommandHandler Handler, CategoryCoreTests.FakeCategoryRepository Repository, StorageStub Storage, SessionStub Session, Category Category) Create()
    {
        var repository = new CategoryCoreTests.FakeCategoryRepository();
        var category = Category.Create("Món chính", "mon-chinh");
        repository.Add(category);
        var storage = new StorageStub();
        var session = new SessionStub();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new UploadCategoryImageCommandHandler(
            repository, session, cache, storage, NullLogger<UploadCategoryImageCommandHandler>.Instance);
        return (handler, repository, storage, session, category);
    }

    private sealed class SessionStub : CulinaryBlog.Application.Abstractions.Persistence.IDataSession
    {
        public int SaveCount { get; private set; }
        public bool FailNextSave { get; set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave) throw new InvalidOperationException("save failed");
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class StorageStub : IFileStorageService
    {
        public List<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken cancellationToken = default)
        {
            Uploaded.Add(objectName);
            return Task.FromResult(StorageRoot + objectName);
        }

        public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default)
        {
            Deleted.Add(publicUrl);
            return Task.CompletedTask;
        }

        public Task<StoredFileInfo?> GetInfoAsync(string publicUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> OpenReadAsync(string publicUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
