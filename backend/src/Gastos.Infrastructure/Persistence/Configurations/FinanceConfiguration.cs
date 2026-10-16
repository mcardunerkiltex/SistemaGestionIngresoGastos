using Gastos.Domain.Accounts;
using Gastos.Domain.Auditing;
using Gastos.Domain.Budgets;
using Gastos.Domain.Categories;
using Gastos.Domain.Transactions;
using Gastos.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gastos.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.Property(x => x.Name).HasMaxLength(80);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.UserId);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Name).HasMaxLength(80);
        b.Property(x => x.Icon).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(9);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UserId, x.Type });
    }
}

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.Property(x => x.Note).HasMaxLength(500);
        b.Ignore(x => x.SignedAmount);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        // Los dos índices que usa el dashboard: movimientos del mes y gasto por categoría.
        b.HasIndex(x => new { x.UserId, x.Date });
        b.HasIndex(x => new { x.UserId, x.CategoryId, x.Date });
        b.HasIndex(x => x.TransferId).HasFilter("transfer_id IS NOT NULL");
        b.ToTable(t => t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0"));
    }
}

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> b)
    {
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.Month, x.CategoryId }).IsUnique().HasFilter("deleted_at IS NULL");
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_log");
        b.Property(x => x.Entity).HasMaxLength(60);
        b.Property(x => x.Action).HasMaxLength(20);
        b.HasIndex(x => new { x.UserId, x.At });
    }
}
