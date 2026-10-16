namespace Gastos.Application.Common;

/// <summary>Error esperado de un caso de uso, que la API traduce a un código HTTP.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class UnauthorizedException(string message = "Credenciales inválidas.") : AppException(message);

public sealed class ConflictException(string message) : AppException(message);

public sealed class NotFoundException(string message) : AppException(message);
