using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Categories;

public sealed class Category : AggregateRoot
{
    private Category(Guid id, string name, string slug, string? description, string? imageUrl, int orderIndex)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        OrderIndex = orderIndex;
    }

    private Category()
        : base(Guid.Empty)
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public int OrderIndex { get; private set; }

    public static Category Create(string name, string slug, string? description = null, string? imageUrl = null, int orderIndex = 0) =>
        new(
            Guid.NewGuid(),
            NormalizeRequired(name, nameof(name), 100),
            NormalizeRequired(slug, nameof(slug), 120),
            NormalizeOptional(description, nameof(description), 2000),
            NormalizeOptional(imageUrl, nameof(imageUrl), 500),
            ValidateOrderIndex(orderIndex));

    public void Update(string name, string? description, string? imageUrl, int orderIndex)
    {
        Name = NormalizeRequired(name, nameof(name), 100);
        Description = NormalizeOptional(description, nameof(description), 2000);
        ImageUrl = NormalizeOptional(imageUrl, nameof(imageUrl), 500);
        OrderIndex = ValidateOrderIndex(orderIndex);
    }

    private static string NormalizeRequired(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static int ValidateOrderIndex(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return value;
    }
}
