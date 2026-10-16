using Gastos.Application.Abstractions;
using Gastos.Infrastructure.Auth;
using Gastos.Infrastructure.Persistence;
using Gastos.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gastos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditingInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.Section));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

        return services;
    }
}
