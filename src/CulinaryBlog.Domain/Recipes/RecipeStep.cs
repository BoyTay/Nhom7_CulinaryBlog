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
        ArgumentOutOfRangeException.ThrowIfLessThan(stepNumber, 1);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Step description is required.", nameof(description));
        }

        if (timerMinutes is < 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(timerMinutes.Value);
        }

        return new RecipeStep(Guid.NewGuid(), stepNumber, description.Trim(), timerMinutes, imageUrl?.Trim());
    }

    internal void Renumber(int stepNumber) => StepNumber = stepNumber;
}
