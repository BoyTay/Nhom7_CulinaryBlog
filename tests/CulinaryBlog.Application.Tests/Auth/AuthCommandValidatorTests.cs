namespace CulinaryBlog.Application.Tests.Auth;

using CulinaryBlog.Application.Auth.Commands.GoogleLogin;
using CulinaryBlog.Application.Auth.Commands.Login;
using CulinaryBlog.Application.Auth.Commands.RefreshToken;
using CulinaryBlog.Application.Auth.Commands.Register;
using CulinaryBlog.Application.Auth.Commands.UpdateProfile;

public sealed class AuthCommandValidatorTests
{
    [Fact]
    public void RegisterCommandValidatorValidDataPassesValidation()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("Chef John", "chef@culinary.test", "P@ssword123!");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "DisplayName required")]
    [InlineData("A", "DisplayName too short")]
    public void RegisterCommandValidatorInvalidDisplayNameFailsValidation(string displayName, string reason)
    {
        _ = reason;
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(displayName, "chef@culinary.test", "P@ssword123!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.DisplayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@missing-user.com")]
    public void RegisterCommandValidatorInvalidEmailFailsValidation(string email)
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("Chef John", email, "P@ssword123!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("short1!")] // < 8 chars
    [InlineData("nocapital1!")] // no uppercase
    [InlineData("NOLOWERCASE1!")] // no lowercase
    [InlineData("NoNumber!")] // no number
    [InlineData("NoSpecial123")] // no special character
    public void RegisterCommandValidatorWeakPasswordFailsValidation(string password)
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("Chef John", "chef@culinary.test", password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void LoginCommandValidatorValidCredentialsPassesValidation()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@culinary.test", "P@ssword123!");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("invalid-email", "password")]
    [InlineData("user@culinary.test", "")]
    public void LoginCommandValidatorInvalidInputFailsValidation(string email, string password)
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand(email, password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshTokenCommandValidatorValidTokenPassesValidation()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("valid-refresh-token-string");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RefreshTokenCommandValidatorEmptyTokenFailsValidation()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand(string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }

    [Fact]
    public void GoogleLoginCommandValidatorValidIdTokenPassesValidation()
    {
        var validator = new GoogleLoginCommandValidator();
        var command = new GoogleLoginCommand("valid-google-id-token");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GoogleLoginCommandValidatorEmptyIdTokenFailsValidation()
    {
        var validator = new GoogleLoginCommandValidator();
        var command = new GoogleLoginCommand(string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GoogleLoginCommand.IdToken));
    }

    [Fact]
    public void UpdateProfileCommandValidatorValidDataPassesValidation()
    {
        var validator = new UpdateProfileCommandValidator();
        var command = new UpdateProfileCommand("Chef Updated", "https://cdn.culinary.test/avatar.png", "Passionate home cook.");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateProfileCommandValidatorInvalidAvatarUrlFailsValidation()
    {
        var validator = new UpdateProfileCommandValidator();
        var command = new UpdateProfileCommand("Chef Updated", "not-a-valid-url", "Bio");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.AvatarUrl));
    }
}
