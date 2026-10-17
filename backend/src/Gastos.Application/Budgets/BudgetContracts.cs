namespace Gastos.Application.Budgets;

/// <summary>Presupuesto y gasto real de una categoría principal de gasto en el mes.</summary>
public sealed record BudgetLineDto(Guid CategoryId, string Name, string? Icon, string? Color, decimal Budgeted, decimal Spent);

public sealed record BudgetMonthDto(DateOnly Month, IReadOnlyList<BudgetLineDto> Lines, decimal TotalBudgeted, decimal TotalSpent);

public sealed record SetBudgetRequest(decimal Amount);
