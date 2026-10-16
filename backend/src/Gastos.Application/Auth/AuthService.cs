using FluentValidation;
using Gastos.Application.Abstractions;
using Gastos.Application.Common;
using Gastos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Auth;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IGoogleTokenValidator google,
    TimeProvider clock,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<GoogleLoginRequest> googleValidator)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Ya existe una cuenta con ese email.");

        var user = User.CreateWithPassword(email, request.DisplayName, "pending");
        user.SetPasswordHash(passwordHasher.Hash(user, request.Password));
        AddNewUser(user);

        return await IssueSessionAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user?.PasswordHash is null || !passwordHasher.Verify(user, user.PasswordHash, request.Password))
            throw new UnauthorizedException("Email o contraseña incorrectos.");

        return await IssueSessionAsync(user, ct);
    }

    public bool GoogleEnabled => google.IsConfigured;

    public async Task<AuthResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct)
    {
        await googleValidator.ValidateAndThrowAsync(request, ct);
        if (!google.IsConfigured) throw new UnauthorizedException("El inicio de sesión con Google no está configurado.");

        var identity = await google.ValidateAsync(request.IdToken, ct)
            ?? throw new UnauthorizedException("No se pudo validar la cuenta de Google.");
        if (!identity.EmailVerified) throw new UnauthorizedException("El email de Google no está verificado.");

        var email = identity.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == identity.Subject, ct)
                   ?? await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = User.CreateWithGoogle(email, string.IsNullOrWhiteSpace(identity.Name) ? email : identity.Name, identity.Subject);
            AddNewUser(user);
        }
        else if (user.GoogleSubject is null)
        {
            // Mismo email verificado por Google: se vincula a la cuenta existente.
            user.LinkGoogle(identity.Subject);
        }

        return await IssueSessionAsync(user, ct);
    }

    /// <summary>Rota el refresh token: el usado queda revocado y se emite uno nuevo.</summary>
    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new UnauthorizedException("Sesión expirada.");

        var now = clock.GetUtcNow();
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || stored.DeletedAt is not null) throw new UnauthorizedException("Sesión expirada.");

        if (!stored.IsActive(now))
        {
            // Un token ya revocado que vuelve a usarse indica robo: se cierran todas las sesiones del usuario.
            if (stored.RevokedAt is not null) await RevokeAllAsync(stored.UserId, now, ct);
            throw new UnauthorizedException("Sesión expirada.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct)
            ?? throw new UnauthorizedException("Sesión expirada.");

        stored.Revoke(now);
        return await IssueSessionAsync(user, ct);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return;
        stored.Revoke(clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");
        return ToDto(user);
    }

    private void AddNewUser(User user)
    {
        db.Users.Add(user);
        db.Accounts.Add(DefaultCatalog.DefaultAccount(user.Id));
        db.Categories.AddRange(DefaultCatalog.Categories(user.Id));
    }

    private async Task<AuthResult> IssueSessionAsync(User user, CancellationToken ct)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken(user.Id, refresh.Hash, refresh.ExpiresAt));
        await db.SaveChangesAsync(ct);

        return new AuthResult(access.Token, access.ExpiresAt, ToDto(user), refresh.Token, refresh.ExpiresAt);
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await db.RefreshTokens.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var token in active) token.Revoke(now);
        await db.SaveChangesAsync(ct);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.PasswordHash is not null, user.GoogleSubject is not null);
}
