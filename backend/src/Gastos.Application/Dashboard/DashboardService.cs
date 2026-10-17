using Gastos.Application.Abstractions;
using Gastos.Application.Budgets;
using Gastos.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Dashboard;

public sealed class DashboardService(IApplicationDbContext db)
{
    private const int HistoryMonths = 6;

    public async Task<DashboardDto> GetAsync(DateOnly month, CancellationToken ct)
    {
        month = MonthlySpending.FirstDay(month);

        // Ingresos y gastos de los últimos meses (las transferencias no cuentan: no son ni una cosa ni la otra).
        var from = month.AddMonths(-(HistoryMonths - 1));
        var to = month.AddMonths(1);
        var totals = await db.Transactions
            .Where(t => t.Date >= from && t.Date < to && (t.Type == TransactionType.Expense || t.Type == TransactionType.Income))
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        decimal Total(DateOnly m, TransactionType type) =>
            totals.Where(x => x.Year == m.Year && x.Month == m.Month && x.Type == type).Sum(x => x.Total);
        var lastMonths = Enumerable.Range(0, HistoryMonths)
            .Select(i => from.AddMonths(i))
            .Select(m => new MonthTotalsDto(m, Total(m, TransactionType.Income), Total(m, TransactionType.Expense)))
            .ToList();

        var budgets = await db.Budgets.Where(b => b.Month == month).ToDictionaryAsync(b => b.CategoryId, b => b.Amount, ct);
        var spent = await MonthlySpending.ByParentCategoryAsync(db, month, ct);
        var ids = spent.Keys.Union(budgets.Keys).ToList();
        var categories = await MonthlySpending.TopLevelExpenseCategories(db)
            .IgnoreQueryFilters(["SoftDelete"])
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
        var byCategory = categories
            .Select(c => new CategorySpendingDto(c.Id, c.Name, c.Icon, c.Color, spent.GetValueOrDefault(c.Id), budgets.GetValueOrDefault(c.Id)))
            .OrderByDescending(c => c.Spent).ThenBy(c => c.Name)
            .ToList();

        var initial = await db.Accounts.Where(a => !a.IsArchived).SumAsync(a => a.InitialBalance, ct);
        var movements = await db.Transactions
            .Where(t => db.Accounts.Any(a => a.Id == t.AccountId && !a.IsArchived))
            .SumAsync(t => t.Type == TransactionType.Expense || t.Type == TransactionType.TransferOut ? -t.Amount : t.Amount, ct);

        var current = lastMonths[^1];
        return new DashboardDto(month, current.Income, current.Expense, budgets.Values.Sum(), initial + movements, byCategory, lastMonths);
    }
}
