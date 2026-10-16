using FluentValidation;
using Gastos.Application.Abstractions;
using Gastos.Application.Common;
using Gastos.Domain.Categories;
using Gastos.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Transactions;

public sealed class TransactionsService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IValidator<SaveTransactionRequest> saveValidator,
    IValidator<SaveTransferRequest> transferValidator,
    IValidator<TransactionQuery> queryValidator)
{
    public async Task<TransactionPage> ListAsync(TransactionQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);

        var rows = db.Transactions.AsQueryable();
        if (query.From is { } from) rows = rows.Where(t => t.Date >= from);
        if (query.To is { } to) rows = rows.Where(t => t.Date <= to);
        if (query.AccountId is { } accountId) rows = rows.Where(t => t.AccountId == accountId);
        if (query.CategoryId is { } categoryId)
        {
            // Filtrar por una categoría incluye sus subcategorías.
            var ids = await db.Categories.Where(c => c.Id == categoryId || c.ParentId == categoryId).Select(c => c.Id).ToListAsync(ct);
            rows = rows.Where(t => t.CategoryId != null && ids.Contains(t.CategoryId.Value));
        }
        rows = query.Kind switch
        {
            TransactionKind.Expense => rows.Where(t => t.Type == TransactionType.Expense),
            TransactionKind.Income => rows.Where(t => t.Type == TransactionType.Income),
            TransactionKind.Transfer => rows.Where(t => t.Type == TransactionType.TransferOut),
            // Una transferencia se lista una vez: por su salida, salvo que se filtre por la cuenta destino.
            _ => query.AccountId is null ? rows.Where(t => t.Type != TransactionType.TransferIn) : rows,
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            rows = rows.Where(t => t.Note != null && t.Note.ToLower().Contains(search));
        }

        var totals = await rows
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0,
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0,
            })
            .FirstOrDefaultAsync(ct);

        var page = await rows
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(ct);

        var items = await ToDtosAsync(page, ct);
        return new TransactionPage(items, totals?.Count ?? 0, query.Page, query.PageSize, totals?.Income ?? 0, totals?.Expense ?? 0);
    }

    public async Task<TransactionDto> GetAsync(Guid id, CancellationToken ct)
    {
        var row = await FindAsync(id, ct);
        if (row.Type == TransactionType.TransferIn)
            row = await db.Transactions.FirstAsync(t => t.TransferId == row.TransferId && t.Type == TransactionType.TransferOut, ct);
        return (await ToDtosAsync([row], ct))[0];
    }

    public async Task<TransactionDto> CreateAsync(SaveTransactionRequest request, CancellationToken ct)
    {
        await saveValidator.ValidateAndThrowAsync(request, ct);
        await EnsureAccountAsync(request.AccountId, ct);
        await EnsureCategoryAsync(request.CategoryId, request.Kind, ct);

        var userId = currentUser.RequiredId();
        var row = request.Kind == TransactionKind.Expense
            ? Transaction.Expense(userId, request.AccountId, request.CategoryId, request.Amount, request.Date, request.Note)
            : Transaction.Income(userId, request.AccountId, request.CategoryId, request.Amount, request.Date, request.Note);
        db.Transactions.Add(row);
        await db.SaveChangesAsync(ct);
        return await GetAsync(row.Id, ct);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, SaveTransactionRequest request, CancellationToken ct)
    {
        await saveValidator.ValidateAndThrowAsync(request, ct);
        var row = await FindAsync(id, ct);
        var current = row.Type == TransactionType.Expense ? TransactionKind.Expense : TransactionKind.Income;
        if (row.IsTransfer || current != request.Kind)
            throw new ConflictException("No se puede cambiar el tipo de un movimiento. Borralo y cargalo de nuevo.");

        if (row.AccountId != request.AccountId) await EnsureAccountAsync(request.AccountId, ct);
        await EnsureCategoryAsync(request.CategoryId, request.Kind, ct);
        row.Update(request.AccountId, request.CategoryId, request.Amount, request.Date, request.Note);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<TransactionDto> CreateTransferAsync(SaveTransferRequest request, CancellationToken ct)
    {
        await transferValidator.ValidateAndThrowAsync(request, ct);
        await EnsureAccountAsync(request.FromAccountId, ct);
        await EnsureAccountAsync(request.ToAccountId, ct);

        var (outRow, inRow) = Transaction.Transfer(currentUser.RequiredId(), request.FromAccountId, request.ToAccountId, request.Amount, request.Date, request.Note);
        db.Transactions.AddRange(outRow, inRow);
        await db.SaveChangesAsync(ct);
        return await GetAsync(outRow.Id, ct);
    }

    public async Task<TransactionDto> UpdateTransferAsync(Guid id, SaveTransferRequest request, CancellationToken ct)
    {
        await transferValidator.ValidateAndThrowAsync(request, ct);
        var (outRow, inRow) = await FindTransferAsync(id, ct);
        if (outRow.AccountId != request.FromAccountId) await EnsureAccountAsync(request.FromAccountId, ct);
        if (inRow.AccountId != request.ToAccountId) await EnsureAccountAsync(request.ToAccountId, ct);

        outRow.UpdateTransferLeg(request.FromAccountId, request.Amount, request.Date, request.Note);
        inRow.UpdateTransferLeg(request.ToAccountId, request.Amount, request.Date, request.Note);
        await db.SaveChangesAsync(ct);
        return await GetAsync(outRow.Id, ct);
    }

    /// <summary>Borrar una parte de una transferencia borra las dos.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var row = await FindAsync(id, ct);
        if (row.IsTransfer)
        {
            var (outRow, inRow) = await FindTransferAsync(id, ct);
            db.Transactions.RemoveRange(outRow, inRow);
        }
        else
        {
            db.Transactions.Remove(row);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Transaction> FindAsync(Guid id, CancellationToken ct) =>
        await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Movimiento no encontrado.");

    private async Task<(Transaction Out, Transaction In)> FindTransferAsync(Guid id, CancellationToken ct)
    {
        var row = await FindAsync(id, ct);
        if (!row.IsTransfer) throw new NotFoundException("Transferencia no encontrada.");
        var legs = await db.Transactions.Where(t => t.TransferId == row.TransferId).ToListAsync(ct);
        return (legs.Single(t => t.Type == TransactionType.TransferOut), legs.Single(t => t.Type == TransactionType.TransferIn));
    }

    private async Task EnsureAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Cuenta no encontrada.");
        if (account.IsArchived) throw new ConflictException($"La cuenta \"{account.Name}\" está archivada.");
    }

    private async Task EnsureCategoryAsync(Guid categoryId, TransactionKind kind, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new NotFoundException("Categoría no encontrada.");
        var expected = kind == TransactionKind.Expense ? CategoryType.Expense : CategoryType.Income;
        if (category.Type != expected)
            throw new ConflictException(kind == TransactionKind.Expense ? "Elegí una categoría de gastos." : "Elegí una categoría de ingresos.");
    }

    private async Task<List<TransactionDto>> ToDtosAsync(IReadOnlyList<Transaction> rows, CancellationToken ct)
    {
        var transferIds = rows.Where(r => r.TransferId is not null).Select(r => r.TransferId!.Value).ToList();
        var legs = transferIds.Count == 0
            ? []
            : await db.Transactions.Where(t => t.TransferId != null && transferIds.Contains(t.TransferId.Value))
                .Select(t => new { TransferId = t.TransferId!.Value, t.Type, t.AccountId })
                .ToListAsync(ct);
        var transferAccounts = legs.GroupBy(l => l.TransferId).ToDictionary(
            g => g.Key,
            g => (From: g.First(l => l.Type == TransactionType.TransferOut).AccountId, To: g.First(l => l.Type == TransactionType.TransferIn).AccountId));

        // Cuentas y categorías se leen incluyendo archivadas o borradas, para mostrar bien movimientos viejos.
        var accounts = await db.Accounts.IgnoreQueryFilters([SoftDelete]).ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var categories = await db.Categories.IgnoreQueryFilters([SoftDelete]).ToDictionaryAsync(c => c.Id, ct);

        return rows.Select(r =>
        {
            CategoryRef? category = null;
            if (r.CategoryId is { } cid && categories.TryGetValue(cid, out var c))
            {
                var parent = c.ParentId is { } pid && categories.TryGetValue(pid, out var p) ? p : null;
                category = new CategoryRef(c.Id, c.Name, parent?.Name, c.Icon ?? parent?.Icon, c.Color ?? parent?.Color);
            }

            var kind = r.Type switch
            {
                TransactionType.Expense => TransactionKind.Expense,
                TransactionType.Income => TransactionKind.Income,
                _ => TransactionKind.Transfer,
            };
            // Para una transferencia, Account es el origen y ToAccount el destino, venga de la fila que venga.
            if (r.TransferId is { } transferId && transferAccounts.TryGetValue(transferId, out var pair))
                return new TransactionDto(r.Id, kind, r.Amount, r.Date, r.Note, Ref(pair.From, accounts), null, Ref(pair.To, accounts));

            return new TransactionDto(r.Id, kind, r.Amount, r.Date, r.Note, Ref(r.AccountId, accounts), category, null);
        }).ToList();
    }

    /// <summary>Nombre del filtro de borrado lógico definido en Infrastructure (AppDbContext.SoftDeleteFilter).</summary>
    private const string SoftDelete = "SoftDelete";

    private static AccountRef Ref(Guid id, Dictionary<Guid, string> names) => new(id, names.GetValueOrDefault(id, "(cuenta borrada)"));
}
