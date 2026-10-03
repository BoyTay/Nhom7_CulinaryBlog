namespace CulinaryBlog.Application.Auth.Commands.UpdateProfile;

using FluentValidation;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .NotEmpty().WithMessage("Tên hiển thị không được để khoảng trắng rỗng.")
                .MinimumLength(2).WithMessage("Tên hiển thị phải có ít nhất 2 ký tự.")
                .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl), () =>
        {
            RuleFor(x => x.AvatarUrl!)
                .MaximumLength(500).WithMessage("Avatar URL tối đa 500 ký tự.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Avatar URL phải là đường dẫn URL hợp lệ.");
        });

        When(x => x.Bio is not null, () =>
        {
            RuleFor(x => x.Bio!)
                .MaximumLength(1000).WithMessage("Tiểu sử tối đa 1000 ký tự.");
        });
    }
}
