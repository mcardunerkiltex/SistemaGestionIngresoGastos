using Gastos.Api.Infrastructure;
using Gastos.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gastos.Api.Controllers;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);

public sealed record AuthConfigResponse(string? GoogleClientId);

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimits.Auth)]
public sealed class AuthController(AuthService auth, IConfiguration configuration, IHostEnvironment env) : ControllerBase
{
    // Todos los endpoints son públicos salvo /me, que usa la política por defecto (sesión requerida).
    /// <summary>Cookie httpOnly con el refresh token. JavaScript no puede leerla.</summary>
    public const string RefreshCookie = "gastos_refresh";

    /// <summary>Qué métodos de inicio de sesión están habilitados.</summary>
    [HttpGet("config")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public AuthConfigResponse Config() =>
        new(auth.GoogleEnabled ? configuration["Google:ClientId"] : null);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct) =>
        Respond(await auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) =>
        Respond(await auth.LoginAsync(request, ct));

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<AuthResponse> Google(GoogleLoginRequest request, CancellationToken ct) =>
        Respond(await auth.LoginWithGoogleAsync(request, ct));

    /// <summary>Renueva el access token usando la cookie. El frontend lo llama al abrir la app y cuando vence.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<AuthResponse> Refresh(CancellationToken ct)
    {
        try
        {
            return Respond(await auth.RefreshAsync(Request.Cookies[RefreshCookie], ct));
        }
        catch
        {
            ClearCookie();
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<NoContentResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(Request.Cookies[RefreshCookie], ct);
        ClearCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [DisableRateLimiting]
    public Task<UserDto> Me(CancellationToken ct) => auth.GetUserAsync(User.UserId(), ct);

    private AuthResponse Respond(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookie, result.RefreshToken, CookieOptions(result.RefreshTokenExpiresAt));
        return new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, result.User);
    }

    private void ClearCookie() => Response.Cookies.Delete(RefreshCookie, CookieOptions(null));

    private CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = !env.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        Expires = expires,
    };
}
