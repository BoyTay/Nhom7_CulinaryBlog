namespace CulinaryBlog.Application.Auth.Commands.GoogleLogin;

using FluentValidation;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token không được để trống.");
    }
}
