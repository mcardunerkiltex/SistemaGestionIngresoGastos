using Gastos.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    /// <summary>Resumen del mes de <paramref name="month"/> (por defecto, el actual).</summary>
    [HttpGet]
    public Task<DashboardDto> Get([FromQuery] DateOnly? month, CancellationToken ct) =>
        dashboard.GetAsync(month ?? DateOnly.FromDateTime(DateTime.Today), ct);
}
