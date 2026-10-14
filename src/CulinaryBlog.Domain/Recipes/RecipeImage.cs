using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeImage : Entity
{
    private RecipeImage(
        Guid id,
        string originalUrl,
        string? mediumUrl,
        string? thumbnailUrl,
        string? altText,
        bool isPrimary,
        int orderIndex)
        : base(id)
    {
        OriginalUrl = originalUrl;
        MediumUrl = mediumUrl;
        ThumbnailUrl = thumbnailUrl;
        AltText = altText;
        IsPrimary = isPrimary;
        OrderIndex = orderIndex;
    }

    private RecipeImage()
        : base(Guid.Empty)
    {
        OriginalUrl = string.Empty;
    }

    public string Url => OriginalUrl;

    public string OriginalUrl { get; private set; }

    public string? MediumUrl { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public string? AltText { get; private set; }

    public bool IsPrimary { get; private set; }

    public int OrderIndex { get; private set; }

    internal static RecipeImage Create(string originalUrl, string? altText, bool isPrimary, int orderIndex)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Image URL must be an absolute URL.", nameof(originalUrl));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(orderIndex);

        return new RecipeImage(Guid.NewGuid(), originalUrl, null, null, altText?.Trim(), isPrimary, orderIndex);
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    internal void SetVariants(string? mediumUrl, string? thumbnailUrl)
    {
        MediumUrl = mediumUrl;
        ThumbnailUrl = thumbnailUrl;
    }

    internal void Reorder(int orderIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(orderIndex);
        OrderIndex = orderIndex;
    }
}
