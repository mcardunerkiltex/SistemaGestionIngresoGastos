using FluentValidation;

namespace Gastos.Application.Accounts;

internal sealed class SaveAccountRequestValidator : AbstractValidator<SaveAccountRequest>
{
    public SaveAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80).WithName("Nombre");
        RuleFor(x => x.Type).IsInEnum().WithName("Tipo");
        RuleFor(x => x.InitialBalance).InclusiveBetween(-1_000_000_000_000m, 1_000_000_000_000m).WithName("Saldo inicial");
    }
}
