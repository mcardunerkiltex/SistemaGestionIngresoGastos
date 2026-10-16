using Gastos.Domain.Accounts;
using Gastos.Domain.Budgets;
using Gastos.Domain.Categories;
using Gastos.Domain.Transactions;
using Gastos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Abstractions;

/// <summary>
/// Acceso a datos que usan los casos de uso. Infrastructure lo implementa con EF Core y PostgreSQL;
/// las consultas ya vienen filtradas por usuario y sin registros borrados.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Budget> Budgets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
