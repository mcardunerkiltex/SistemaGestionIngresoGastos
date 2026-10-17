using Gastos.Application.Abstractions;
using Gastos.Domain.Categories;
using Gastos.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Budgets;

/// <summary>Gasto del mes sumado por categoría principal (lo de una subcategoría cuenta para su categoría).</summary>
internal static class MonthlySpending
{
    public static DateOnly FirstDay(DateOnly date) => new(date.Year, date.Month, 1);

    public static async Task<Dictionary<Guid, decimal>> ByParentCategoryAsync(IApplicationDbContext db, DateOnly month, CancellationToken ct)
    {
        var from = FirstDay(month);
        var to = from.AddMonths(1);
        var byCategory = await db.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= from && t.Date < to && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        if (byCategory.Count == 0) return [];

        // Las categorías borradas siguen contando: el gasto existió.
        var ids = byCategory.Select(x => x.CategoryId).ToList();
        var parents = await db.Categories
            .IgnoreQueryFilters(["SoftDelete"])
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.ParentId ?? c.Id, ct);

        return byCategory
            .GroupBy(x => parents.GetValueOrDefault(x.CategoryId, x.CategoryId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));
    }

    public static IQueryable<Category> TopLevelExpenseCategories(IApplicationDbContext db) =>
        db.Categories.Where(c => c.Type == CategoryType.Expense && c.ParentId == null);
}
