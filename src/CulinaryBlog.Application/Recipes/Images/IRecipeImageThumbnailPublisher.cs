namespace CulinaryBlog.Application.Recipes.Images;

public interface IRecipeImageThumbnailPublisher
{
    string Publish(RecipeImageThumbnailRequested request);
}
