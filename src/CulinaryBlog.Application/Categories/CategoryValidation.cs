using FluentValidation;

namespace CulinaryBlog.Application.Categories;

internal static class CategoryValidation
{
    public static void AddNameRules<T>(IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .Must(name => name is not null && name.Trim().Length is >= 2 and <= 50)
        .WithMessage("Name must contain 2 to 50 characters.")
        .Must(name => name is not null && !name.Contains('<') && !name.Contains('>'))
        .WithMessage("Name must not contain HTML.")
        .Must(name => name is not null && CategorySlug.Generate(name).Length > 0)
        .WithMessage("Name must produce a valid slug.");

    public static void AddOptionalRules<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string?>> description,
        System.Linq.Expressions.Expression<Func<T, string?>> imageUrl,
        System.Linq.Expressions.Expression<Func<T, int>> orderIndex)
    {
        validator.RuleFor(description).MaximumLength(2000);
        validator.RuleFor(imageUrl).MaximumLength(500);
        validator.RuleFor(orderIndex).GreaterThanOrEqualTo(0);
    }
}
