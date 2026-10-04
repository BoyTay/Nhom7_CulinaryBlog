namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class ForbiddenException(string message, string code = "FORBIDDEN") : Exception(message)
{
    public string Code { get; } = code;
}
