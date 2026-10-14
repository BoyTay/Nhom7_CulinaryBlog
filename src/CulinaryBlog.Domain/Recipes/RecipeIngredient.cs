using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeIngredient : Entity
{
    private RecipeIngredient(Guid id, string name, decimal quantity, string unit, string? notes, int sortOrder)
        : base(id)
    {
        Name = name;
        Quantity = quantity;
        Unit = unit;
        Notes = notes;
        SortOrder = sortOrder;
    }

    private RecipeIngredient()
        : base(Guid.Empty)
    {
        Name = string.Empty;
        Unit = string.Empty;
    }

    public string Name { get; private set; }

    public decimal Quantity { get; private set; }

    public string Unit { get; private set; }

    public string? Notes { get; private set; }

    public int SortOrder { get; private set; }

    internal static RecipeIngredient Create(
        string name,
        decimal quantity,
        string unit,
        string? notes,
        int sortOrder)
    {
        Validate(name, quantity, unit, notes);

        ArgumentOutOfRangeException.ThrowIfLessThan(sortOrder, 1);

        return new RecipeIngredient(
            Guid.NewGuid(),
            name.Trim(),
            quantity,
            unit.Trim(),
            notes?.Trim(),
            sortOrder);
    }

    internal void Update(string name, decimal quantity, string unit, string? notes)
    {
        Validate(name, quantity, unit, notes);
        Name = name.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        Notes = notes?.Trim();
    }

    internal void Reorder(int sortOrder) => SortOrder = sortOrder;

    private static void Validate(string name, decimal quantity, string unit, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ArgumentException("Ingredient name must contain 1 to 200 characters.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 50)
        {
            throw new ArgumentException("Ingredient unit must contain 1 to 50 characters.", nameof(unit));
        }

        if (notes is { Length: > 500 })
        {
            throw new ArgumentException("Ingredient notes cannot exceed 500 characters.", nameof(notes));
        }
    }
}
