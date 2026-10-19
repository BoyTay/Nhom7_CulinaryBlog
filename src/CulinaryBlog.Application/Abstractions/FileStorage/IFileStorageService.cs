namespace CulinaryBlog.Application.Abstractions.FileStorage;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string publicUrl,
        CancellationToken cancellationToken = default);

    // Returns null for a missing object; rejects URLs outside the configured storage.
    Task<StoredFileInfo?> GetInfoAsync(
        string publicUrl,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string publicUrl,
        CancellationToken cancellationToken = default);
}
