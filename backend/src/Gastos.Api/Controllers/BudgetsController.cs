using Gastos.Application.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

/// <summary>Presupuesto mensual. El mes va como fecha ("2026-10-01"); se toma el mes de esa fecha.</summary>
[ApiController]
[Route("api/v1/budgets/{month}")]
public sealed class BudgetsController(BudgetsService budgets) : ControllerBase
{
    [HttpGet]
    public Task<BudgetMonthDto> Get(DateOnly month, CancellationToken ct) => budgets.GetMonthAsync(month, ct);

    [HttpPut("{categoryId:guid}")]
    public async Task<NoContentResult> Set(DateOnly month, Guid categoryId, SetBudgetRequest request, CancellationToken ct)
    {
        await budgets.SetAsync(month, categoryId, request, ct);
        return NoContent();
    }

    [HttpPost("copy-previous")]
    public Task<BudgetMonthDto> CopyPrevious(DateOnly month, CancellationToken ct) => budgets.CopyPreviousAsync(month, ct);
}
