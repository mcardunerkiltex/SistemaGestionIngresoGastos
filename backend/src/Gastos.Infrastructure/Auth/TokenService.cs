using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gastos.Application.Abstractions;
using Gastos.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gastos.Infrastructure.Auth;

internal sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            ]),
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });
        return new AccessToken(token, expires);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return new IssuedRefreshToken(token, HashRefreshToken(token), clock.GetUtcNow().AddDays(_options.RefreshTokenDays));
    }

    /// <summary>Solo se guarda el hash: un dump de la base no permite reusar sesiones.</summary>
    public string HashRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
