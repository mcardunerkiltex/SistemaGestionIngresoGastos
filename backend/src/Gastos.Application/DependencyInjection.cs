using FluentValidation;
using Gastos.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Gastos.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y validadores de todos los módulos.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<AuthService>();
        return services;
    }
}
