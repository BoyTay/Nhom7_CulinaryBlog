namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class NotFoundException(string message, string code = "NOT_FOUND") : Exception(message)
{
    public string Code { get; } = code;
}
