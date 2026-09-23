namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeNutrition
{
    private RecipeNutrition()
    {
    }

    private RecipeNutrition(int calories, decimal proteinGrams, decimal carbohydratesGrams, decimal fatGrams)
    {
        Calories = calories;
        ProteinGrams = proteinGrams;
        CarbohydratesGrams = carbohydratesGrams;
        FatGrams = fatGrams;
    }

    public int Calories { get; private set; }

    public decimal ProteinGrams { get; private set; }

    public decimal CarbohydratesGrams { get; private set; }

    public decimal FatGrams { get; private set; }

    public static RecipeNutrition Create(
        int calories,
        decimal proteinGrams,
        decimal carbohydratesGrams,
        decimal fatGrams)
    {
        if (calories < 0 || proteinGrams < 0 || carbohydratesGrams < 0 || fatGrams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calories), "Nutrition values cannot be negative.");
        }

        return new RecipeNutrition(calories, proteinGrams, carbohydratesGrams, fatGrams);
    }
}
