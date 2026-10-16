using System.Security.Claims;
using Gastos.Application.Abstractions;

namespace Gastos.Api.Infrastructure;

/// <summary>Lee el usuario del JWT del request ("sub").</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? Id =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? accessor.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;
}
