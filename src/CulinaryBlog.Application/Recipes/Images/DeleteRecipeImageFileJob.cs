using CulinaryBlog.Application.Abstractions.FileStorage;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed class DeleteRecipeImageFileJob(IFileStorageService fileStorage)
{
    public Task ExecuteAsync(string publicUrl, CancellationToken cancellationToken = default) =>
        fileStorage.DeleteAsync(publicUrl, cancellationToken);
}
