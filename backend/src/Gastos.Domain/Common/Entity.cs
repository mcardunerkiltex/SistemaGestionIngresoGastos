namespace Gastos.Domain.Common;

/// <summary>Base de todas las entidades: identidad UUID, auditoría de fechas y borrado lógico.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}

/// <summary>Entidad que pertenece a un usuario. Toda consulta se filtra por UserId.</summary>
public abstract class UserOwnedEntity : Entity
{
    public Guid UserId { get; protected set; }
}
