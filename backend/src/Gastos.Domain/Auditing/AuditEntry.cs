namespace Gastos.Domain.Auditing;

/// <summary>Registro de un cambio sobre una entidad del usuario.</summary>
public sealed class AuditEntry
{
    public long Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Entity { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = default!;
    public DateTimeOffset At { get; private set; }

    private AuditEntry() { }

    public AuditEntry(Guid? userId, string entity, Guid entityId, string action, DateTimeOffset at)
    {
        UserId = userId;
        Entity = entity;
        EntityId = entityId;
        Action = action;
        At = at;
    }
}
