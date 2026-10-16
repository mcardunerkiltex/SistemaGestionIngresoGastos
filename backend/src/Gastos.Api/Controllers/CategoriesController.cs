using Gastos.Application.Categories;
using Gastos.Domain.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(CategoriesService categories) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoryDto>> List([FromQuery] CategoryType? type, CancellationToken ct) =>
        categories.ListAsync(type, ct);

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await categories.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public Task<CategoryDto> Update(Guid id, UpdateCategoryRequest request, CancellationToken ct) =>
        categories.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> Delete(Guid id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
