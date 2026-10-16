using Gastos.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Gastos.Api.Infrastructure;

/// <summary>Traduce las reglas de negocio violadas a un 400 con ProblemDetails.</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException) return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Title = "Regla de negocio", Detail = exception.Message, Status = 400 },
        });
    }
}
