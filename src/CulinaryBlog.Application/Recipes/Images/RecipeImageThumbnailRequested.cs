namespace CulinaryBlog.Application.Recipes.Images;

public sealed record RecipeImageThumbnailRequested(
    Guid RecipeId,
    Guid RecipeImageId,
    string OriginalUrl,
    string IdempotencyKey);
