using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeBySlug;

public sealed class GetRecipeBySlugQueryValidator : AbstractValidator<GetRecipeBySlugQuery>
{
    public GetRecipeBySlugQueryValidator()
    {
        RuleFor(query => query.Slug)
            .NotEmpty()
            .MaximumLength(200);
    }
}
