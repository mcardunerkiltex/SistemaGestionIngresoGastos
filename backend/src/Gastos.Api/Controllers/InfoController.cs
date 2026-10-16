using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/info")]
public sealed class InfoController(IHostEnvironment env) : ControllerBase
{
    /// <summary>Versión y entorno de la API. Lo usa el frontend para verificar la conexión.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        name = "Gastos API",
        version = typeof(InfoController).Assembly.GetName().Version?.ToString(3),
        environment = env.EnvironmentName,
    });
}
