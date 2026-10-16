using Gastos.Domain.Common;

namespace Gastos.Domain.Transactions;

/// <summary>
/// Un movimiento de dinero. El monto siempre es positivo; el tipo define si suma o resta.
/// Una transferencia son dos filas (salida y entrada) unidas por TransferId.
/// </summary>
public sealed class Transaction : UserOwnedEntity
{
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Note { get; private set; }
    public Guid? TransferId { get; private set; }

    private Transaction() { }

    public static Transaction Expense(Guid userId, Guid accountId, Guid categoryId, decimal amount, DateOnly date, string? note = null) =>
        Create(userId, accountId, categoryId, TransactionType.Expense, amount, date, note, null);

    public static Transaction Income(Guid userId, Guid accountId, Guid categoryId, decimal amount, DateOnly date, string? note = null) =>
        Create(userId, accountId, categoryId, TransactionType.Income, amount, date, note, null);

    /// <summary>Devuelve la salida de la cuenta origen y la entrada en la cuenta destino.</summary>
    public static (Transaction Out, Transaction In) Transfer(Guid userId, Guid fromAccountId, Guid toAccountId, decimal amount, DateOnly date, string? note = null)
    {
        if (fromAccountId == toAccountId) throw new DomainException("La cuenta origen y destino deben ser distintas.");
        var transferId = Guid.CreateVersion7();
        return (
            Create(userId, fromAccountId, null, TransactionType.TransferOut, amount, date, note, transferId),
            Create(userId, toAccountId, null, TransactionType.TransferIn, amount, date, note, transferId));
    }

    /// <summary>Efecto sobre el saldo de la cuenta: negativo para gastos y transferencias salientes.</summary>
    public decimal SignedAmount => Type is TransactionType.Expense or TransactionType.TransferOut ? -Amount : Amount;

    private static Transaction Create(Guid userId, Guid accountId, Guid? categoryId, TransactionType type, decimal amount, DateOnly date, string? note, Guid? transferId)
    {
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        return new Transaction
        {
            UserId = userId,
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = decimal.Round(amount, 2),
            Date = date,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            TransferId = transferId,
        };
    }
}
