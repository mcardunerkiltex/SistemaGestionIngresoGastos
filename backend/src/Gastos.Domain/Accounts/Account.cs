using Gastos.Domain.Common;

namespace Gastos.Domain.Accounts;

/// <summary>Donde está la plata: efectivo, banco, tarjeta o billetera virtual.</summary>
public sealed class Account : UserOwnedEntity
{
    public string Name { get; private set; } = default!;
    public AccountType Type { get; private set; }
    public string Currency { get; private set; } = "ARS";
    public decimal InitialBalance { get; private set; }
    public bool IsArchived { get; private set; }

    private Account() { }

    public Account(Guid userId, string name, AccountType type, decimal initialBalance, string currency = "ARS")
    {
        UserId = userId;
        Rename(name);
        Type = type;
        InitialBalance = initialBalance;
        Currency = currency;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("La cuenta necesita un nombre.");
        Name = name.Trim();
    }

    public void Update(string name, AccountType type, decimal initialBalance)
    {
        Rename(name);
        Type = type;
        InitialBalance = decimal.Round(initialBalance, 2);
    }

    public void SetArchived(bool archived) => IsArchived = archived;
}
