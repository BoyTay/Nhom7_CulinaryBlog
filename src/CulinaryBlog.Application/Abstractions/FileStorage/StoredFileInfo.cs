namespace CulinaryBlog.Application.Abstractions.FileStorage;

public sealed record StoredFileInfo(
    string ObjectName,
    string ContentType,
    long ContentLength,
    string? ETag);
