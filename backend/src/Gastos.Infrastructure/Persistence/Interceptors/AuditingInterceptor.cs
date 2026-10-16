using Gastos.Application.Abstractions;
using Gastos.Domain.Auditing;
using Gastos.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gastos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa CreatedAt/UpdatedAt, convierte los DELETE en borrado lógico y deja un registro en audit_log.
/// </summary>
public sealed class AuditingInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context) Apply(context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context) Apply(context);
        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext context)
    {
        var now = clock.GetUtcNow();
        var audit = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries<Entity>().ToList())
        {
            string? action = null;
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    action = "created";
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    action = "updated";
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    action = "deleted";
                    break;
            }

            if (action is not null)
                audit.Add(new AuditEntry(currentUser.Id, entry.Metadata.ClrType.Name, entry.Entity.Id, action, now));
        }

        context.Set<AuditEntry>().AddRange(audit);
    }
}
