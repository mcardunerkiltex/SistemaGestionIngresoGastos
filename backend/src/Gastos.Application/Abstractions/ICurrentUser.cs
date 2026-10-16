namespace Gastos.Application.Abstractions;

/// <summary>Usuario autenticado del request actual.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }
}
