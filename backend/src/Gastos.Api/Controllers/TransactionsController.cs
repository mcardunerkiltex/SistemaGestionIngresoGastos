using Gastos.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public sealed class TransactionsController(TransactionsService transactions) : ControllerBase
{
    /// <summary>Movimientos paginados, del más nuevo al más viejo, con los totales del filtro.</summary>
    [HttpGet]
    public Task<TransactionPage> List([FromQuery] TransactionQuery query, CancellationToken ct) =>
        transactions.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<TransactionDto> Get(Guid id, CancellationToken ct) => transactions.GetAsync(id, ct);

    /// <summary>Alta de un gasto o ingreso.</summary>
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(SaveTransactionRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await transactions.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public Task<TransactionDto> Update(Guid id, SaveTransactionRequest request, CancellationToken ct) =>
        transactions.UpdateAsync(id, request, ct);

    [HttpPost("transfers")]
    public async Task<ActionResult<TransactionDto>> CreateTransfer(SaveTransferRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await transactions.CreateTransferAsync(request, ct));

    [HttpPut("transfers/{id:guid}")]
    public Task<TransactionDto> UpdateTransfer(Guid id, SaveTransferRequest request, CancellationToken ct) =>
        transactions.UpdateTransferAsync(id, request, ct);

    /// <summary>Borra un movimiento. Si es una transferencia, borra las dos partes.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid id, CancellationToken ct)
    {
        await transactions.DeleteAsync(id, ct);
        return NoContent();
    }
}
