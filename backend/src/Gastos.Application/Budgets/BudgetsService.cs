using FluentValidation;
using Gastos.Application.Abstractions;
using Gastos.Application.Common;
using Gastos.Domain.Budgets;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Budgets;

/// <summary>
/// Presupuesto mensual por categoría principal de gasto. Un monto en 0 equivale a no tener presupuesto.
/// </summary>
public sealed class BudgetsService(IApplicationDbContext db, ICurrentUser currentUser, IValidator<SetBudgetRequest> validator)
{
    public async Task<BudgetMonthDto> GetMonthAsync(DateOnly month, CancellationToken ct)
    {
        month = MonthlySpending.FirstDay(month);
        var categories = await MonthlySpending.TopLevelExpenseCategories(db).OrderBy(c => c.Name).ToListAsync(ct);
        var budgets = await db.Budgets.Where(b => b.Month == month).ToDictionaryAsync(b => b.CategoryId, b => b.Amount, ct);
        var spent = await MonthlySpending.ByParentCategoryAsync(db, month, ct);

        var lines = categories
            .Select(c => new BudgetLineDto(c.Id, c.Name, c.Icon, c.Color, budgets.GetValueOrDefault(c.Id), spent.GetValueOrDefault(c.Id)))
            .OrderByDescending(l => l.Budgeted > 0).ThenByDescending(l => l.Spent).ThenBy(l => l.Name)
            .ToList();

        // Gasto en categorías que ya no existen: se suma al total aunque no tenga línea propia.
        var totalSpent = spent.Values.Sum();
        return new BudgetMonthDto(month, lines, lines.Sum(l => l.Budgeted), totalSpent);
    }

    public async Task SetAsync(DateOnly month, Guid categoryId, SetBudgetRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        month = MonthlySpending.FirstDay(month);
        if (!await MonthlySpending.TopLevelExpenseCategories(db).AnyAsync(c => c.Id == categoryId, ct))
            throw new NotFoundException("El presupuesto se arma sobre una categoría principal de gasto.");

        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Month == month && b.CategoryId == categoryId, ct);
        if (request.Amount == 0)
        {
            if (budget is not null) db.Budgets.Remove(budget);
        }
        else if (budget is null)
        {
            db.Budgets.Add(new Budget(currentUser.RequiredId(), month, categoryId, request.Amount));
        }
        else
        {
            budget.SetAmount(request.Amount);
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Copia el presupuesto del mes anterior a las categorías que todavía no tienen uno este mes.</summary>
    public async Task<BudgetMonthDto> CopyPreviousAsync(DateOnly month, CancellationToken ct)
    {
        month = MonthlySpending.FirstDay(month);
        var previous = await db.Budgets.Where(b => b.Month == month.AddMonths(-1)).ToListAsync(ct);
        if (previous.Count == 0) throw new NotFoundException("El mes anterior no tiene presupuesto para copiar.");

        var existing = await db.Budgets.Where(b => b.Month == month).Select(b => b.CategoryId).ToListAsync(ct);
        var userId = currentUser.RequiredId();
        foreach (var b in previous.Where(b => !existing.Contains(b.CategoryId)))
            db.Budgets.Add(new Budget(userId, month, b.CategoryId, b.Amount));
        await db.SaveChangesAsync(ct);
        return await GetMonthAsync(month, ct);
    }
}
