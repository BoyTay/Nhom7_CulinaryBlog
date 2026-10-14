using CulinaryBlog.Application.Abstractions.Messaging;

namespace CulinaryBlog.Application.Recipes.Images;

public sealed record UploadRecipeImageCommand(
    Guid RecipeId,
    Stream Content,
    long ContentLength,
    string ContentType,
    string? AltText,
    bool IsPrimary,
    uint Version) : ICommand<Guid>;
