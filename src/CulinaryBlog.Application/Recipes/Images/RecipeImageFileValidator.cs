using System.Net.Http.Headers;
using CulinaryBlog.Application.Common.Exceptions;

namespace CulinaryBlog.Application.Recipes.Images;

public static class RecipeImageFileValidator
{
    public const long MaximumFileSizeBytes = 5 * 1024 * 1024;

    private const int HeaderLength = 12;

    private static readonly Dictionary<string, string> ExtensionsByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/avif"] = ".avif",
        };

    public static async Task<string> ValidateAsync(
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (contentLength <= 0)
        {
            throw new FileUploadValidationException("The uploaded file is empty.", "FILE_EMPTY");
        }

        if (contentLength > MaximumFileSizeBytes)
        {
            throw new FileUploadValidationException(
                "The uploaded file exceeds the 5 MB size limit.",
                "FILE_SIZE_EXCEEDED");
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            throw new FileUploadValidationException(
                "The uploaded image content type is not allowed.",
                "FILE_MIME_INVALID");
        }

        var normalizedContentType = mediaType.MediaType?.ToLowerInvariant() ?? string.Empty;
        if (!ExtensionsByContentType.TryGetValue(normalizedContentType, out var extension))
        {
            throw new FileUploadValidationException(
                "The uploaded image content type is not allowed.",
                "FILE_MIME_INVALID");
        }

        if (!content.CanSeek)
        {
            throw new InvalidOperationException("The image stream must support seeking for signature validation.");
        }

        if (content.Length > MaximumFileSizeBytes)
        {
            throw new FileUploadValidationException(
                "The uploaded file exceeds the 5 MB size limit.",
                "FILE_SIZE_EXCEEDED");
        }

        var originalPosition = content.Position;
        var header = new byte[HeaderLength];
        int bytesRead;
        try
        {
            content.Position = 0;
            bytesRead = await content.ReadAtLeastAsync(
                header.AsMemory(),
                HeaderLength,
                throwOnEndOfStream: false,
                cancellationToken);
        }
        finally
        {
            content.Position = originalPosition;
        }

        if (!HasMatchingSignature(normalizedContentType, header, bytesRead))
        {
            throw new FileUploadValidationException(
                "The uploaded file signature does not match its content type.",
                "FILE_SIGNATURE_INVALID");
        }

        return extension;
    }

    private static bool HasMatchingSignature(string contentType, byte[] header, int bytesRead) => contentType switch
    {
        "image/jpeg" => bytesRead >= 3 && header.AsSpan(0, 3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
        "image/png" => bytesRead >= 8
            && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytesRead >= HeaderLength
            && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        "image/avif" => bytesRead >= HeaderLength
            && header.AsSpan(4, 4).SequenceEqual("ftyp"u8)
            && (header.AsSpan(8, 4).SequenceEqual("avif"u8)
                || header.AsSpan(8, 4).SequenceEqual("avis"u8)),
        _ => false,
    };
}
