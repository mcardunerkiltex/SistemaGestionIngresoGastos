using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gastos.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "gastos-api";
    public string Audience { get; set; } = "gastos-web";

    /// <summary>Clave HMAC de al menos 32 caracteres. En producción va como secreto, nunca en el repo.</summary>
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;

    public SymmetricSecurityKey CreateSigningKey()
    {
        if (SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 caracteres.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    }
}

public sealed class GoogleAuthOptions
{
    public const string Section = "Google";

    /// <summary>OAuth Client ID (tipo "Aplicación web") de Google Cloud. Vacío = login con Google desactivado.</summary>
    public string ClientId { get; set; } = "";
}
