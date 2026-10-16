using System.Linq.Expressions;
using Gastos.Application.Abstractions;
using Gastos.Domain.Accounts;
using Gastos.Domain.Auditing;
using Gastos.Domain.Budgets;
using Gastos.Domain.Categories;
using Gastos.Domain.Common;
using Gastos.Domain.Transactions;
using Gastos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IApplicationDbContext
{
    /// <summary>Filtro que limita cada consulta a los datos del usuario autenticado.</summary>
    public const string OwnerFilter = "Owner";

    /// <summary>Filtro que oculta los registros con borrado lógico.</summary>
    public const string SoftDeleteFilter = "SoftDelete";

    // Leído por los filtros en cada consulta (EF lo parametriza por instancia del contexto).
    private Guid? CurrentUserId => currentUser.Id;

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (typeof(UserOwnedEntity).IsAssignableFrom(clr))
            {
                GetType().GetMethod(nameof(ApplyOwnerFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clr).Invoke(this, [modelBuilder]);
            }
            if (typeof(Entity).IsAssignableFrom(clr))
            {
                GetType().GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clr).Invoke(this, [modelBuilder]);
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }

    private void ApplyOwnerFilter<T>(ModelBuilder modelBuilder) where T : UserOwnedEntity =>
        modelBuilder.Entity<T>().HasQueryFilter(OwnerFilter, e => e.UserId == CurrentUserId);

    private void ApplySoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : Entity =>
        modelBuilder.Entity<T>().HasQueryFilter(SoftDeleteFilter, e => e.DeletedAt == null);
}
