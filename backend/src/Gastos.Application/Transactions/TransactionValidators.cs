using FluentValidation;

namespace Gastos.Application.Transactions;

internal static class AmountRules
{
    public const decimal Max = 1_000_000_000_000m;

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("El monto debe ser mayor a cero.")
            .LessThan(Max).WithMessage("El monto es demasiado grande.");

    public static IRuleBuilderOptions<T, DateOnly> ValidDate<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).WithMessage("La fecha no es válida.");
}

internal sealed class SaveTransactionRequestValidator : AbstractValidator<SaveTransactionRequest>
{
    public SaveTransactionRequestValidator()
    {
        RuleFor(x => x.Kind).Must(k => k is TransactionKind.Expense or TransactionKind.Income)
            .WithMessage("Para transferencias usá /transactions/transfers.");
        RuleFor(x => x.AccountId).NotEmpty().WithName("Cuenta");
        RuleFor(x => x.CategoryId).NotEmpty().WithName("Categoría");
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.Date).ValidDate();
        RuleFor(x => x.Note).MaximumLength(500).WithName("Nota");
    }
}

internal sealed class SaveTransferRequestValidator : AbstractValidator<SaveTransferRequest>
{
    public SaveTransferRequestValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty().WithName("Cuenta de origen");
        RuleFor(x => x.ToAccountId).NotEmpty().NotEqual(x => x.FromAccountId)
            .WithMessage("La cuenta de destino debe ser distinta a la de origen.");
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.Date).ValidDate();
        RuleFor(x => x.Note).MaximumLength(500).WithName("Nota");
    }
}

internal sealed class TransactionQueryValidator : AbstractValidator<TransactionQuery>
{
    public TransactionQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From is not null && x.To is not null)
            .WithMessage("La fecha final debe ser posterior a la inicial.");
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
