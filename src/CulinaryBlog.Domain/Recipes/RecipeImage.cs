using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeImage : Entity
{
    private RecipeImage(Guid id, string url, string? altText, bool isPrimary)
        : base(id)
    {
        Url = url;
        AltText = altText;
        IsPrimary = isPrimary;
    }

    private RecipeImage()
        : base(Guid.Empty)
    {
        Url = string.Empty;
    }

    public string Url { get; private set; }

    public string? AltText { get; private set; }

    public bool IsPrimary { get; private set; }

    internal static RecipeImage Create(string url, string? altText, bool isPrimary)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Image URL must be an absolute URL.", nameof(url));
        }

        return new RecipeImage(Guid.NewGuid(), url, altText?.Trim(), isPrimary);
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
