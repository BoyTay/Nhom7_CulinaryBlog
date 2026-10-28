namespace CulinaryBlog.Application.Auth.Commands.UploadAvatar;

using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Application.Recipes.Images;
using Microsoft.Extensions.Logging;

public sealed class UploadAvatarCommandHandler(
    ICurrentUser currentUser,
    IIdentityService identityService,
    IFileStorageService fileStorage,
    ILogger<UploadAvatarCommandHandler> logger) : ICommandHandler<UploadAvatarCommand, AuthUserDto>
{
    private static readonly Action<ILogger, Exception?> LogOldAvatarCleanupFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1302, "AvatarCleanupFailed"),
            "Previous avatar file could not be deleted; orphan file reconciliation is required.");

    public async Task<AuthUserDto> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Người dùng chưa được xác thực.", "UNAUTHORIZED");
        }

        var user = await identityService.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng.", "USER_NOT_FOUND");

        // Avatars accept the same formats, size limit and signature checks as recipe images.
        var extension = await RecipeImageFileValidator.ValidateAsync(
            request.Content,
            request.ContentType,
            request.ContentLength,
            cancellationToken);
        var avatarPrefix = AvatarPrefix(userId);
        var avatarUrl = await fileStorage.UploadAsync(
            request.Content,
            $"{avatarPrefix}{Guid.NewGuid():N}{extension}",
            request.ContentType,
            cancellationToken);

        AuthUserDto updated;
        try
        {
            updated = await identityService.UpdateProfileAsync(userId, null, avatarUrl, null, cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(avatarUrl);
            throw;
        }

        if (user.AvatarUrl is not null && user.AvatarUrl.Contains($"/{avatarPrefix}", StringComparison.Ordinal))
        {
            await TryDeleteAsync(user.AvatarUrl);
        }

        return updated;
    }

    private static string AvatarPrefix(string userId) => $"avatars/{userId}/";

    private async Task TryDeleteAsync(string url)
    {
        try
        {
            // Cleanup must still run when the HTTP request has been cancelled.
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await fileStorage.DeleteAsync(url, cleanupTimeout.Token);
        }
        catch (Exception exception) when (exception is FileStorageUnavailableException or OperationCanceledException or ArgumentException)
        {
            LogOldAvatarCleanupFailure(logger, exception);
        }
    }
}
