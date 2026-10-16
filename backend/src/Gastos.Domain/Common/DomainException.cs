namespace Gastos.Domain.Common;

/// <summary>Violación de una regla de negocio.</summary>
public sealed class DomainException(string message) : Exception(message);
