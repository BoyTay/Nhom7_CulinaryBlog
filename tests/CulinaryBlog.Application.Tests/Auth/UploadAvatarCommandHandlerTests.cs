using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Auth.Commands.UploadAvatar;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Application.Tests.Auth;

public sealed class UploadAvatarCommandHandlerTests
{
    private const string UserId = "user-1";
    private const string StorageRoot = "http://localhost/media/culinary-blog/";
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    [Fact]
    public async Task StoresAvatarUnderUserPrefixUpdatesProfileAndDeletesPreviousOwnAvatar()
    {
        var previous = $"{StorageRoot}avatars/{UserId}/old.png";
        var identity = new IdentityStub(previous);
        var storage = new StorageStub();
        var handler = CreateHandler(identity, storage);
        using var content = new MemoryStream(PngHeader);

        var result = await handler.Handle(new UploadAvatarCommand(content, content.Length, "image/png"), CancellationToken.None);

        var uploaded = Assert.Single(storage.Uploaded);
        Assert.StartsWith($"avatars/{UserId}/", uploaded, StringComparison.Ordinal);
        Assert.EndsWith(".png", uploaded, StringComparison.Ordinal);
        Assert.Equal(StorageRoot + uploaded, result.AvatarUrl);
        Assert.Equal([previous], storage.Deleted);
    }

    [Fact]
    public async Task KeepsExternalAvatarThatDoesNotBelongToStorage()
    {
        var identity = new IdentityStub("https://images.example.test/me.png");
        var storage = new StorageStub();
        using var content = new MemoryStream(PngHeader);

        await CreateHandler(identity, storage).Handle(
            new UploadAvatarCommand(content, content.Length, "image/png"),
            CancellationToken.None);

        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task RejectsFileWhoseSignatureDoesNotMatchBeforeUploading()
    {
        var storage = new StorageStub();
        using var content = new MemoryStream("<svg onload=alert(1)>"u8.ToArray());

        var exception = await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            CreateHandler(new IdentityStub(null), storage).Handle(
                new UploadAvatarCommand(content, content.Length, "image/png"),
                CancellationToken.None));

        Assert.Equal("FILE_SIGNATURE_INVALID", exception.Code);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task RemovesUploadedFileWhenProfileUpdateFails()
    {
        var identity = new IdentityStub(null) { FailUpdate = true };
        var storage = new StorageStub();
        using var content = new MemoryStream(PngHeader);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler(identity, storage).Handle(
                new UploadAvatarCommand(content, content.Length, "image/png"),
                CancellationToken.None));

        Assert.Equal(storage.Uploaded.Select(name => StorageRoot + name), storage.Deleted);
    }

    private static UploadAvatarCommandHandler CreateHandler(IdentityStub identity, StorageStub storage) =>
        new(new CurrentUserStub(), identity, storage, NullLogger<UploadAvatarCommandHandler>.Instance);

    private sealed class CurrentUserStub : ICurrentUser
    {
        public string? UserId => UploadAvatarCommandHandlerTests.UserId;
        public string? Email => "chef@example.test";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
        public IReadOnlyList<string> Roles => [];
        public bool IsAdmin => false;
        public bool IsAuthor => true;
    }

    private sealed class StorageStub : IFileStorageService
    {
        public List<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken cancellationToken = default)
        {
            Uploaded.Add(objectName);
            return Task.FromResult(StorageRoot + objectName);
        }

        public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default)
        {
            Deleted.Add(publicUrl);
            return Task.CompletedTask;
        }

        public Task<StoredFileInfo?> GetInfoAsync(string publicUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> OpenReadAsync(string publicUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class IdentityStub(string? avatarUrl) : IIdentityService
    {
        public bool FailUpdate { get; init; }

        public Task<AuthUserDto?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthUserDto?>(User(avatarUrl));

        public Task<AuthUserDto> UpdateProfileAsync(string userId, string? displayName, string? avatarUrl, string? bio, CancellationToken cancellationToken = default) =>
            FailUpdate
                ? throw new ConflictException("Cập nhật thông tin thất bại.", "UPDATE_PROFILE_FAILED")
                : Task.FromResult(User(avatarUrl));

        public Task<AuthUserDto> RegisterAsync(string displayName, string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoogleSignInResult> FindOrCreateGoogleUserAsync(string idToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsLockedOutAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PasswordResetTicket?> CreatePasswordResetTicketAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static AuthUserDto User(string? avatar) => new(UserId, "chef@example.test", "Chef", avatar, null, ["Author"]);
    }
}
