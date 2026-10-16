using FluentValidation;
using Gastos.Application.Accounts;
using Gastos.Application.Auth;
using Gastos.Application.Categories;
using Gastos.Application.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Gastos.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y validadores de todos los módulos.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<AuthService>();
        services.AddScoped<AccountsService>();
        services.AddScoped<CategoriesService>();
        services.AddScoped<TransactionsService>();
        return services;
    }
}
