using Gastos.Domain.Users;

namespace Gastos.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string hash, string password);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    IssuedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string token);
}

/// <summary>Datos verificados de un ID token de Google.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string Name);

public interface IGoogleTokenValidator
{
    bool IsConfigured { get; }

    /// <returns>La identidad, o null si el token no es válido.</returns>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
