using FluentValidation;
using Gastos.Application.Common;
using Gastos.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gastos.Api.Infrastructure;

/// <summary>Traduce los errores esperados de la aplicación a respuestas ProblemDetails.</summary>
public sealed class AppExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors.GroupBy(e => ToCamelCase(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Title = "Hay datos inválidos.",
                Status = StatusCodes.Status400BadRequest,
            },
            DomainException => Problem(StatusCodes.Status400BadRequest, "Regla de negocio", exception.Message),
            UnauthorizedException => Problem(StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflicto", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "No encontrado", exception.Message),
            _ => null,
        };
        if (problem is null) return false;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
