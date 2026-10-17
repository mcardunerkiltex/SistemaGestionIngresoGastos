namespace Gastos.Application.Dashboard;

public sealed record CategorySpendingDto(Guid CategoryId, string Name, string? Icon, string? Color, decimal Spent, decimal Budgeted);

public sealed record MonthTotalsDto(DateOnly Month, decimal Income, decimal Expense);

/// <summary>Resumen del mes para la pantalla de inicio.</summary>
public sealed record DashboardDto(
    DateOnly Month,
    decimal Income,
    decimal Expense,
    decimal Budgeted,
    decimal TotalBalance,
    IReadOnlyList<CategorySpendingDto> ByCategory,
    IReadOnlyList<MonthTotalsDto> LastMonths);
