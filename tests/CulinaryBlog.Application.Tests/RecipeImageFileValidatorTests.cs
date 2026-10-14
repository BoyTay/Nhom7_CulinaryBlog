using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Images;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeImageFileValidatorTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/avif", ".avif")]
    public async Task ValidateAsyncAcceptsAllowedSignaturesAndPreservesStreamPosition(
        string contentType,
        string expectedExtension)
    {
        using var content = new MemoryStream(GetHeader(contentType));
        content.Position = 2;

        var extension = await RecipeImageFileValidator.ValidateAsync(
            content,
            contentType,
            content.Length);

        Assert.Equal(expectedExtension, extension);
        Assert.Equal(2, content.Position);
    }

    [Fact]
    public async Task ValidateAsyncAcceptsContentTypeCaseInsensitively()
    {
        using var content = new MemoryStream(GetHeader("image/jpeg"));

        var extension = await RecipeImageFileValidator.ValidateAsync(
            content,
            "IMAGE/JPEG",
            content.Length);

        Assert.Equal(".jpg", extension);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("image/gif")]
    public async Task ValidateAsyncRejectsUnsupportedContentTypes(string contentType)
    {
        using var content = new MemoryStream(new byte[16]);

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, contentType, content.Length));

        Assert.Equal("FILE_MIME_INVALID", exception.Code);
    }

    [Fact]
    public async Task ValidateAsyncRejectsEmptyFiles()
    {
        using var content = new MemoryStream();

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, "image/png", 0));

        Assert.Equal("FILE_EMPTY", exception.Code);
    }

    [Fact]
    public async Task ValidateAsyncRejectsFilesOverFiveMegabytes()
    {
        using var content = new MemoryStream(new byte[1]);

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(
                content,
                "image/jpeg",
                RecipeImageFileValidator.MaximumFileSizeBytes + 1));

        Assert.Equal("FILE_SIZE_EXCEEDED", exception.Code);
    }

    [Theory]
    [InlineData("image/jpeg", "image/png")]
    [InlineData("image/png", "image/jpeg")]
    public async Task ValidateAsyncRejectsMismatchedMagicBytes(string declaredType, string actualType)
    {
        using var content = new MemoryStream(GetHeader(actualType));

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, declaredType, content.Length));

        Assert.Equal("FILE_SIGNATURE_INVALID", exception.Code);
    }

    [Fact]
    public async Task ValidateAsyncAcceptsExactlyFiveMegabytes()
    {
        var bytes = new byte[RecipeImageFileValidator.MaximumFileSizeBytes];
        GetHeader("image/png").CopyTo(bytes, 0);
        using var content = new MemoryStream(bytes);

        Assert.Equal(".png", await RecipeImageFileValidator.ValidateAsync(content, "image/png", content.Length));
    }

    [Fact]
    public async Task ValidateAsyncChecksActualLengthEvenWhenDeclaredLengthIsSmall()
    {
        using var content = new MemoryStream(new byte[RecipeImageFileValidator.MaximumFileSizeBytes + 1]);

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, "image/png", 12));

        Assert.Equal("FILE_SIZE_EXCEEDED", exception.Code);
    }

    [Theory]
    [InlineData("image/jpeg", 2)]
    [InlineData("image/png", 7)]
    [InlineData("image/webp", 11)]
    [InlineData("image/avif", 11)]
    public async Task ValidateAsyncRejectsTruncatedHeaders(string contentType, int length)
    {
        using var content = new MemoryStream(GetHeader(contentType)[..length]);

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, contentType, content.Length));

        Assert.Equal("FILE_SIGNATURE_INVALID", exception.Code);
    }

    [Theory]
    [InlineData("image/webp")]
    [InlineData("image/avif")]
    public async Task ValidateAsyncRejectsWrongContainerBrand(string contentType)
    {
        var bytes = GetHeader(contentType);
        bytes[8] = 0;
        using var content = new MemoryStream(bytes);

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(
            () => RecipeImageFileValidator.ValidateAsync(content, contentType, content.Length));

        Assert.Equal("FILE_SIGNATURE_INVALID", exception.Code);
    }

    private static byte[] GetHeader(string contentType) => contentType switch
    {
        "image/jpeg" => [0xFF, 0xD8, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        "image/png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0],
        "image/webp" => [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50],
        "image/avif" => [0, 0, 0, 0, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66],
        _ => throw new ArgumentOutOfRangeException(nameof(contentType)),
    };
}
