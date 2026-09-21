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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ingredient name is required.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Ingredient unit is required.", nameof(unit));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(sortOrder, 1);

        return new RecipeIngredient(
            Guid.NewGuid(),
            name.Trim(),
            quantity,
            unit.Trim(),
            notes?.Trim(),
            sortOrder);
    }

    internal void Reorder(int sortOrder) => SortOrder = sortOrder;
}