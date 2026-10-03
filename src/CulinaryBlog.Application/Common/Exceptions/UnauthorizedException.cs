namespace CulinaryBlog.Application.Common.Exceptions;

public sealed class UnauthorizedException(string message, string code = "UNAUTHORIZED") : Exception(message)
{
    public string Code { get; } = code;
}
