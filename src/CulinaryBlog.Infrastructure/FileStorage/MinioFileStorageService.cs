using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.FileStorage;

public sealed class MinioFileStorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration) : IFileStorageService, IDisposable
{
    private static readonly string[] PublicReadActions = ["s3:GetObject"];
    private readonly string _bucketName = configuration["Minio:BucketName"]
        ?? throw new InvalidOperationException("MinIO bucket name is not configured.");
    private readonly Uri _publicEndpoint = ParseEndpoint(configuration["Minio:PublicEndpoint"]
        ?? configuration["Minio:Endpoint"]
        ?? throw new InvalidOperationException("MinIO public endpoint is not configured."));
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public async Task<string> UploadAsync(
        Stream content,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ValidateObjectName(objectName);

        try
        {
            await EnsureBucketAsync(cancellationToken);
            if (content.CanSeek)
            {
                content.Position = 0;
            }
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectName,
                InputStream = content,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                AutoCloseStream = false,
            }, cancellationToken);

            return BuildPublicUrl(objectName);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageUnavailableException("MinIO could not store the uploaded image.", exception);
        }
    }

    public async Task DeleteAsync(
        string publicUrl,
        CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(publicUrl);

        try
        {
            await s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectName,
            }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // A file that is already absent satisfies the delete contract.
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageUnavailableException("MinIO could not delete the image.", exception);
        }
    }

    public async Task<StoredFileInfo?> GetInfoAsync(
        string publicUrl,
        CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(publicUrl);
        try
        {
            var metadata = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = objectName,
            }, cancellationToken);
            return new StoredFileInfo(objectName, metadata.Headers.ContentType,
                metadata.Headers.ContentLength, metadata.ETag);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageUnavailableException("MinIO could not read the image metadata.", exception);
        }
    }

    public async Task<Stream?> OpenReadAsync(
        string publicUrl,
        CancellationToken cancellationToken = default)
    {
        var objectName = GetObjectName(publicUrl);
        try
        {
            var response = await s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectName,
            }, cancellationToken);
            return new OwnedResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageUnavailableException("MinIO could not read the image.", exception);
        }
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady)
            {
                return;
            }

            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, _bucketName))
            {
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName }, cancellationToken);
            }

            await s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = _bucketName,
                Policy = JsonSerializer.Serialize(new
                {
                    Version = "2012-10-17",
                    Statement = new[]
                    {
                        new
                        {
                            Effect = "Allow",
                            Principal = "*",
                            Action = PublicReadActions,
                            Resource = $"arn:aws:s3:::{_bucketName}/*",
                        },
                    },
                }),
            }, cancellationToken);

            _bucketReady = true;
        }
        catch (AmazonS3Exception exception)
        {
            throw new FileStorageUnavailableException("MinIO bucket initialization failed.", exception);
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private string BuildPublicUrl(string objectName)
    {
        var escapedObjectName = string.Join(
            '/',
            objectName.Split('/').Select(Uri.EscapeDataString));
        var path = $"{_publicEndpoint.AbsolutePath.TrimEnd('/')}/{Uri.EscapeDataString(_bucketName)}/{escapedObjectName}";
        return new UriBuilder(_publicEndpoint) { Path = path }.Uri.AbsoluteUri;
    }

    private string GetObjectName(string publicUrl)
    {
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var fileUri)
            || !string.Equals(fileUri.Scheme, _publicEndpoint.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(fileUri.Authority, _publicEndpoint.Authority, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The image URL does not belong to this MinIO endpoint.", nameof(publicUrl));
        }

        var basePath = _publicEndpoint.AbsolutePath.TrimEnd('/');
        var filePath = fileUri.AbsolutePath;
        var bucketPath = Uri.EscapeDataString(_bucketName);
        var expectedPrefix = $"{basePath}/{bucketPath}/";
        if (!filePath.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The image URL does not belong to the configured MinIO bucket.", nameof(publicUrl));
        }

        var encodedObjectName = filePath[expectedPrefix.Length..];
        var objectName = string.Join(
            '/',
            encodedObjectName.Split('/').Select(Uri.UnescapeDataString));
        ValidateObjectName(objectName);
        return objectName;
    }

    private static Uri ParseEndpoint(string endpoint)
    {
        var endpointWithScheme = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : $"http://{endpoint}";

        if (!Uri.TryCreate(endpointWithScheme, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("MinIO endpoint must be an absolute HTTP or HTTPS URL.");
        }

        return uri;
    }

    private static void ValidateObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)
            || objectName.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("A valid MinIO object name is required.", nameof(objectName));
        }
    }

    public void Dispose() => _bucketLock.Dispose();

    private sealed class OwnedResponseStream(GetObjectResponse response) : Stream
    {
        private Stream Inner => response.ResponseStream;

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            Inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            Inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            Inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync() => base.DisposeAsync();
    }
}
