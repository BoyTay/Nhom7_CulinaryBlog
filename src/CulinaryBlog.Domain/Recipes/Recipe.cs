using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Exceptions;

namespace CulinaryBlog.Domain.Recipes;

public sealed class Recipe : AggregateRoot
{
    private readonly List<RecipeStep> _steps = [];
    private readonly List<RecipeIngredient> _ingredients = [];
    private readonly List<RecipeImage> _images = [];

    private Recipe(
        Guid id,
        string title,
        string slug,
        string description,
        Guid categoryId,
        string authorId,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        RecipeDifficulty difficulty)
        : base(id)
    {
        Title = title;
        Slug = slug;
        Description = description;
        CategoryId = categoryId;
        AuthorId = authorId;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
        Status = RecipeStatus.Draft;
    }

    private Recipe()
        : base(Guid.Empty)
    {
        Title = string.Empty;
        Slug = string.Empty;
        Description = string.Empty;
        AuthorId = string.Empty;
    }

    public string Title { get; private set; }

    public string Slug { get; private set; }

    public string Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public string AuthorId { get; private set; }

    public int PrepTimeMinutes { get; private set; }

    public int CookTimeMinutes { get; private set; }

    public int Servings { get; private set; }

    public RecipeDifficulty Difficulty { get; private set; }

    public RecipeStatus Status { get; private set; }

    public RecipeNutrition? Nutrition { get; private set; }

    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();

    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    public IReadOnlyCollection<RecipeImage> Images => _images.AsReadOnly();

    public static Recipe Create(
        string title,
        string slug,
        string description,
        Guid categoryId,
        string authorId,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        RecipeDifficulty difficulty)
    {
        ValidateText(title, nameof(title));
        ValidateText(slug, nameof(slug));
        ValidateText(description, nameof(description));
        ValidateText(authorId, nameof(authorId));
        ValidateTime(prepTimeMinutes, nameof(prepTimeMinutes));
        ValidateTime(cookTimeMinutes, nameof(cookTimeMinutes));

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(servings, 1);

        return new Recipe(
            Guid.NewGuid(),
            title.Trim(),
            slug.Trim(),
            description.Trim(),
            categoryId,
            authorId.Trim(),
            prepTimeMinutes,
            cookTimeMinutes,
            servings,
            difficulty);
    }

    public void Update(
        string title,
        string slug,
        string description,
        Guid categoryId,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        RecipeDifficulty difficulty,
        RecipeNutrition? nutrition)
    {
        ValidateText(title, nameof(title));
        ValidateText(slug, nameof(slug));
        ValidateText(description, nameof(description));
        ValidateTime(prepTimeMinutes, nameof(prepTimeMinutes));
        ValidateTime(cookTimeMinutes, nameof(cookTimeMinutes));

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(servings, 1);

        Title = title.Trim();
        Slug = slug.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
        Nutrition = nutrition;
    }

    public void Publish()
    {
        if (_steps.Count == 0 || _ingredients.Count == 0)
        {
            throw new DomainException(
                "RECIPE_NOT_READY",
                "A recipe must have at least one step and one ingredient before publishing.");
        }

        Status = RecipeStatus.Published;
    }

    public void Unpublish() => Status = RecipeStatus.Draft;

    public void Archive() => Status = RecipeStatus.Archived;

    public RecipeStep AddStep(string description, int? timerMinutes = null, string? imageUrl = null)
    {
        var step = RecipeStep.Create(_steps.Count + 1, description, timerMinutes, imageUrl);
        _steps.Add(step);
        return step;
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.SingleOrDefault(item => item.Id == stepId)
            ?? throw new DomainException("STEP_NOT_FOUND", "The recipe step was not found.");

        _steps.Remove(step);
        for (var index = 0; index < _steps.Count; index++)
        {
            _steps[index].Renumber(index + 1);
        }
    }

    public RecipeIngredient AddIngredient(
        string name,
        decimal quantity,
        string unit,
        string? notes = null)
    {
        var ingredient = RecipeIngredient.Create(name, quantity, unit, notes, _ingredients.Count + 1);
        _ingredients.Add(ingredient);
        return ingredient;
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        var ingredient = _ingredients.SingleOrDefault(item => item.Id == ingredientId)
            ?? throw new DomainException("INGREDIENT_NOT_FOUND", "The recipe ingredient was not found.");

        _ingredients.Remove(ingredient);
        for (var index = 0; index < _ingredients.Count; index++)
        {
            _ingredients[index].Reorder(index + 1);
        }
    }

    public RecipeImage AddImage(string url, string? altText = null)
    {
        var image = RecipeImage.Create(url, altText, _images.Count == 0);
        _images.Add(image);
        return image;
    }

    public void SetPrimaryImage(Guid imageId)
    {
        if (_images.All(image => image.Id != imageId))
        {
            throw new DomainException("IMAGE_NOT_FOUND", "The recipe image was not found.");
        }

        foreach (var image in _images)
        {
            image.SetPrimary(image.Id == imageId);
        }
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.SingleOrDefault(item => item.Id == imageId)
            ?? throw new DomainException("IMAGE_NOT_FOUND", "The recipe image was not found.");
        var wasPrimary = image.IsPrimary;

        _images.Remove(image);
        if (wasPrimary && _images.Count > 0)
        {
            _images[0].SetPrimary(true);
        }
    }

    private static void ValidateText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }

    private static void ValidateTime(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
