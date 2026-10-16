using FluentValidation;
using Gastos.Application.Abstractions;
using Gastos.Application.Common;
using Gastos.Domain.Accounts;
using Gastos.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Accounts;

public sealed class AccountsService(IApplicationDbContext db, ICurrentUser currentUser, IValidator<SaveAccountRequest> validator)
{
    /// <summary>Cuentas con su saldo actual: saldo inicial más la suma de sus movimientos.</summary>
    public async Task<IReadOnlyList<AccountDto>> ListAsync(bool includeArchived, CancellationToken ct)
    {
        var balances = await db.Transactions
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Total = g.Sum(t => t.Type == TransactionType.Expense || t.Type == TransactionType.TransferOut ? -t.Amount : t.Amount),
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, ct);

        var accounts = await db.Accounts
            .Where(a => includeArchived || !a.IsArchived)
            .OrderBy(a => a.IsArchived).ThenBy(a => a.Name)
            .ToListAsync(ct);

        return accounts.Select(a => ToDto(a, balances.GetValueOrDefault(a.Id))).ToList();
    }

    public async Task<AccountDto> CreateAsync(SaveAccountRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var account = new Account(currentUser.RequiredId(), request.Name, request.Type, request.InitialBalance);
        account.SetArchived(request.IsArchived);
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return ToDto(account, 0m);
    }

    public async Task<AccountDto> UpdateAsync(Guid id, SaveAccountRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var account = await FindAsync(id, ct);
        account.Update(request.Name, request.Type, request.InitialBalance);
        account.SetArchived(request.IsArchived);
        await db.SaveChangesAsync(ct);

        var movements = await db.Transactions.Where(t => t.AccountId == id)
            .SumAsync(t => t.Type == TransactionType.Expense || t.Type == TransactionType.TransferOut ? -t.Amount : t.Amount, ct);
        return ToDto(account, movements);
    }

    /// <summary>Solo se borra una cuenta sin movimientos; si tiene, se archiva para no perder historial.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var account = await FindAsync(id, ct);
        if (await db.Transactions.AnyAsync(t => t.AccountId == id, ct))
            throw new ConflictException("La cuenta tiene movimientos. Archivala para ocultarla sin perder el historial.");
        db.Accounts.Remove(account);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Account> FindAsync(Guid id, CancellationToken ct) =>
        await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException("Cuenta no encontrada.");

    private static AccountDto ToDto(Account a, decimal movements) =>
        new(a.Id, a.Name, a.Type, a.Currency, a.InitialBalance, a.InitialBalance + movements, a.IsArchived);
}
