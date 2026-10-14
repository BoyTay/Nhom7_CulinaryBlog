namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class FileStorageUnavailableException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
