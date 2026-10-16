using Gastos.Application.Abstractions;
using Gastos.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Gastos.Infrastructure.Auth;

/// <summary>PBKDF2 con el hasher de ASP.NET Core Identity (salt por usuario, iteraciones versionadas).</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(User user, string password) => _inner.HashPassword(user, password);

    public bool Verify(User user, string hash, string password) =>
        _inner.VerifyHashedPassword(user, hash, password) != PasswordVerificationResult.Failed;
}
