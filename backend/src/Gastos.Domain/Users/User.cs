using Gastos.Domain.Common;

namespace Gastos.Domain.Users;

public sealed class User : Entity
{
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;

    /// <summary>Null cuando el usuario solo entra con Google.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Identificador "sub" de Google, si vinculó su cuenta.</summary>
    public string? GoogleSubject { get; private set; }

    public string BaseCurrency { get; private set; } = "ARS";

    private User() { }

    public static User CreateWithPassword(string email, string displayName, string passwordHash) =>
        new() { Email = Normalize(email), DisplayName = displayName.Trim(), PasswordHash = passwordHash };

    public static User CreateWithGoogle(string email, string displayName, string googleSubject) =>
        new() { Email = Normalize(email), DisplayName = displayName.Trim(), GoogleSubject = googleSubject };

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void LinkGoogle(string googleSubject) => GoogleSubject = googleSubject;

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
