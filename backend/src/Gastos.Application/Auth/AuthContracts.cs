namespace Gastos.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record GoogleLoginRequest(string IdToken);

public sealed record UserDto(Guid Id, string Email, string DisplayName, bool HasPassword, bool HasGoogle);

/// <summary>Resultado de iniciar sesión. El refresh token viaja en una cookie httpOnly, no en el JSON.</summary>
public sealed record AuthResult(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserDto User, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
