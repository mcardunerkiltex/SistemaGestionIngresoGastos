using Gastos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    /// <summary>Registra un usuario nuevo y devuelve un cliente con su sesión.</summary>
    public async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var client = CreateBrowser();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"user-{Guid.NewGuid():N}@test.com", password = "Clave1234", displayName = "Test" });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User.Id);
    }

    private sealed record SessionResponse(string AccessToken, SessionUser User);
    private sealed record SessionUser(Guid Id);

    /// <summary>Contexto de base de datos actuando como un usuario, para preparar datos de prueba.</summary>
    public AppDbContext DbAs(Guid userId)
    {
        var options = Services.GetRequiredService<DbContextOptions<AppDbContext>>();
        return new AppDbContext(options, new FixedUser(userId));
    }

    private sealed class FixedUser(Guid id) : Gastos.Application.Abstractions.ICurrentUser
    {
        public Guid? Id => id;
    }

    /// <summary>Cliente HTTPS que guarda cookies, como un navegador.</summary>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
}
