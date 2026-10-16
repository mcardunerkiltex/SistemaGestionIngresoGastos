using FluentValidation;

namespace Gastos.Application.Auth;

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254).WithName("Email");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(x => x.Password)
            .NotEmpty().WithName("Contraseña")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(128)
            .Matches("[A-Za-z]").WithMessage("La contraseña debe tener al menos una letra.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.");
    }
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithName("Email");
        RuleFor(x => x.Password).NotEmpty().WithName("Contraseña");
    }
}

internal sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator() => RuleFor(x => x.IdToken).NotEmpty();
}
