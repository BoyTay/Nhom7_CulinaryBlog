using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeStep : Entity
{
    private RecipeStep(Guid id, int stepNumber, string description, int? timerMinutes, string? imageUrl)
        : base(id)
    {
        StepNumber = stepNumber;
        Description = description;
        TimerMinutes = timerMinutes;
        ImageUrl = imageUrl;
    }

    private RecipeStep()
        : base(Guid.Empty)
    {
        Description = string.Empty;
    }

    public int StepNumber { get; private set; }

    public string Description { get; private set; }

    public int? TimerMinutes { get; private set; }

    public string? ImageUrl { get; private set; }

    internal static RecipeStep Create(int stepNumber, string description, int? timerMinutes, string? imageUrl)
    {
        Validate(description, timerMinutes, imageUrl);
        ArgumentOutOfRangeException.ThrowIfLessThan(stepNumber, 1);

        return new RecipeStep(Guid.NewGuid(), stepNumber, description.Trim(), timerMinutes, imageUrl?.Trim());
    }

    internal void Update(string description, int? timerMinutes, string? imageUrl)
    {
        Validate(description, timerMinutes, imageUrl);
        Description = description.Trim();
        TimerMinutes = timerMinutes;
        ImageUrl = imageUrl?.Trim();
    }

    private static void Validate(string description, int? timerMinutes, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Step description is required.", nameof(description));
        }

        if (description.Length > 2000)
        {
            throw new ArgumentException("Step description cannot exceed 2000 characters.", nameof(description));
        }

        if (imageUrl is { Length: > 2048 })
        {
            throw new ArgumentException("Step image URL cannot exceed 2048 characters.", nameof(imageUrl));
        }

        if (timerMinutes is < 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(timerMinutes.Value);
        }
    }

    internal void Renumber(int stepNumber) => StepNumber = stepNumber;
}
