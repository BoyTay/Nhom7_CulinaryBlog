using FluentValidation;

namespace CulinaryBlog.Application.Recipes.Queries.GetRecipeDetail;

public sealed class GetRecipeDetailQueryValidator : AbstractValidator<GetRecipeDetailQuery>
{
    public GetRecipeDetailQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
