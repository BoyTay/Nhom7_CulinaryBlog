using CulinaryBlog.Application.Common.Exceptions;

namespace CulinaryBlog.Application.Tests;

public sealed class ApplicationValidationExceptionTests
{
    [Fact]
    public void ConstructorPreservesFieldErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required."],
        };

        var exception = new ApplicationValidationException(errors);

        Assert.Equal(errors, exception.Errors);
    }
}
