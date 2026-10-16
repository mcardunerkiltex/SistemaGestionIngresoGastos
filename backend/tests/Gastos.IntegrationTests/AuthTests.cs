using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gastos.Application.Abstractions;
using Gastos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gastos.IntegrationTests;

public sealed class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
    private sealed record UserDto(Guid Id, string Email, string DisplayName, bool HasPassword, bool HasGoogle);
    private sealed record AuthConfig(string? GoogleClientId);

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.com";

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email, string password = "Clave1234")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, displayName = "Mariano" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task Register_returns_token_sets_httponly_cookie_and_seeds_defaults()
    {
        var client = factory.CreateBrowser();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = email.ToUpperInvariant(), password = "Clave1234", displayName = "Mariano" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("gastos_refresh=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(email, body.User.Email);
        Assert.True(body.User.HasPassword);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Accounts.IgnoreQueryFilters().CountAsync(a => a.UserId == body.User.Id));
        Assert.True(await db.Categories.IgnoreQueryFilters().CountAsync(c => c.UserId == body.User.Id) > 20);
        var user = await db.Users.SingleAsync(u => u.Id == body.User.Id);
        Assert.DoesNotContain("Clave1234", user.PasswordHash);
    }

    [Fact]
    public async Task Register_twice_with_same_email_is_a_conflict()
    {
        var client = factory.CreateBrowser();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Clave1234", displayName = "Otro" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_field_errors()
    {
        var response = await factory.CreateBrowser().PostAsJsonAsync("/api/v1/auth/register", new { email = "no-es-email", password = "corta", displayName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var errors = problem!["errors"].ToString()!;
        Assert.Contains("email", errors);
        Assert.Contains("password", errors);
        Assert.Contains("displayName", errors);
    }

    [Fact]
    public async Task Login_checks_password()
    {
        var client = factory.CreateBrowser();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var wrong = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Incorrecta1" });
        var right = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Clave1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
    }

    [Fact]
    public async Task Me_requires_a_valid_access_token()
    {
        var client = factory.CreateBrowser();
        var auth = await RegisterAsync(client, NewEmail());

        var anonymous = await client.GetAsync("/api/v1/auth/me");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(auth.User.Id, me!.Id);
    }

    [Fact]
    public async Task Refresh_rotates_the_cookie_and_reuse_of_an_old_token_closes_every_session()
    {
        var client = factory.CreateBrowser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = NewEmail(), password = "Clave1234", displayName = "M" });
        var firstCookie = register.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        var refreshed = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);

        // Un atacante reusa la cookie original, ya rotada.
        var attacker = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var stolen = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        stolen.Headers.Add("Cookie", firstCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.SendAsync(stolen)).StatusCode);

        // La sesión legítima también queda cerrada.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var client = factory.CreateBrowser();
        await RegisterAsync(client, NewEmail());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Google_login_is_disabled_without_client_id()
    {
        var client = factory.CreateBrowser();

        var config = await client.GetFromJsonAsync<AuthConfig>("/api/v1/auth/config");
        var google = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = "x" });

        Assert.Null(config!.GoogleClientId);
        Assert.Equal(HttpStatusCode.Unauthorized, google.StatusCode);
    }

    [Fact]
    public async Task Each_user_only_sees_their_own_data()
    {
        var client = factory.CreateBrowser();
        var alice = await RegisterAsync(client, NewEmail());
        var bob = await RegisterAsync(factory.CreateBrowser(), NewEmail());

        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>>();
        await using var asAlice = new AppDbContext(options, new FixedUser(alice.User.Id));

        var owners = await asAlice.Categories.Select(c => c.UserId).Distinct().ToListAsync();
        Assert.Equal([alice.User.Id], owners);
        Assert.DoesNotContain(bob.User.Id, owners);
    }

    private sealed class FixedUser(Guid id) : ICurrentUser
    {
        public Guid? Id => id;
    }
}
