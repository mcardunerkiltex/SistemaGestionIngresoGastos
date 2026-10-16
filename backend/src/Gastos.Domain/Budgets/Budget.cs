using Gastos.Domain.Common;

namespace Gastos.Domain.Budgets;

/// <summary>Monto planificado para una categoría de gasto en un mes.</summary>
public sealed class Budget : UserOwnedEntity
{
    /// <summary>Primer día del mes al que aplica.</summary>
    public DateOnly Month { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }

    private Budget() { }

    public Budget(Guid userId, DateOnly month, Guid categoryId, decimal amount)
    {
        UserId = userId;
        Month = new DateOnly(month.Year, month.Month, 1);
        CategoryId = categoryId;
        SetAmount(amount);
    }

    public void SetAmount(decimal amount)
    {
        if (amount < 0) throw new DomainException("El presupuesto no puede ser negativo.");
        Amount = decimal.Round(amount, 2);
    }
}
