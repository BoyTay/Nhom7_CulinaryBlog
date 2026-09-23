namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class ConflictException(string message, string code = "CONFLICT") : Exception(message)
{
    public string Code { get; } = code;
}
