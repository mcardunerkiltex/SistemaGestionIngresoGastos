using Gastos.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(AccountsService accounts) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AccountDto>> List([FromQuery] bool includeArchived, CancellationToken ct) =>
        accounts.ListAsync(includeArchived, ct);

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(SaveAccountRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await accounts.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public Task<AccountDto> Update(Guid id, SaveAccountRequest request, CancellationToken ct) =>
        accounts.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid id, CancellationToken ct)
    {
        await accounts.DeleteAsync(id, ct);
        return NoContent();
    }
}
