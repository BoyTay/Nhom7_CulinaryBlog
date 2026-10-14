namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class FileUploadValidationException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
