using Gastos.Application.Abstractions;

namespace Gastos.Application.Common;

public static class CurrentUserExtensions
{
    /// <summary>Id del usuario autenticado. Los endpoints que lo usan ya exigen sesión.</summary>
    public static Guid RequiredId(this ICurrentUser user) =>
        user.Id ?? throw new UnauthorizedException("Sesión requerida.");
}
