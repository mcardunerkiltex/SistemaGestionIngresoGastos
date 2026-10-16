using Gastos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Gastos.IntegrationTests;

/// <summary>
/// Levanta la API completa contra una base PostgreSQL real y descartable.
/// Usa TEST_DATABASE_URL o, si no está, la base local de docker compose.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString;

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
                     ?? "Host=localhost;Port=5432;Username=gastos;Password=gastos";
        _connectionString = new NpgsqlConnectionStringBuilder(server) { Database = $"gastos_test_{Guid.NewGuid():N}" }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("RateLimits:AuthPerMinute", "1000");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }

    /// <summary>Cliente HTTPS que guarda cookies, como un navegador.</summary>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
}
