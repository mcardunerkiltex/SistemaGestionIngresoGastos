using FluentValidation;

namespace Gastos.Application.Budgets;

internal sealed class SetBudgetRequestValidator : AbstractValidator<SetBudgetRequest>
{
    public SetBudgetRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).LessThan(1_000_000_000_000m);
    }
}
