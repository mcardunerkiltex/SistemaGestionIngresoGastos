using Gastos.Domain.Transactions;

namespace Gastos.Application.Transactions;

public sealed record AccountRef(Guid Id, string Name);

public sealed record CategoryRef(Guid Id, string Name, string? ParentName, string? Icon, string? Color);

/// <summary>
/// Un movimiento tal como lo ve el usuario. Una transferencia se muestra como una sola fila
/// con su cuenta de origen y destino.
/// </summary>
public sealed record TransactionDto(
    Guid Id,
    TransactionKind Kind,
    decimal Amount,
    DateOnly Date,
    string? Note,
    AccountRef Account,
    CategoryRef? Category,
    AccountRef? ToAccount);

public enum TransactionKind
{
    Expense = 1,
    Income = 2,
    Transfer = 3,
}

public sealed record TransactionQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? AccountId,
    Guid? CategoryId,
    TransactionKind? Kind,
    string? Search,
    int Page = 1,
    int PageSize = 50);

public sealed record TransactionPage(IReadOnlyList<TransactionDto> Items, int Total, int Page, int PageSize, decimal TotalIncome, decimal TotalExpense);

public sealed record SaveTransactionRequest(TransactionKind Kind, Guid AccountId, Guid CategoryId, decimal Amount, DateOnly Date, string? Note);

public sealed record SaveTransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, DateOnly Date, string? Note);
