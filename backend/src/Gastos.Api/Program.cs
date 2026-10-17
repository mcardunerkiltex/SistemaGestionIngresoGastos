using Gastos.Api.Infrastructure;
using Gastos.Application;
using Gastos.Application.Abstractions;
using Gastos.Infrastructure;
using Gastos.Infrastructure.Auth;
using Gastos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.CreateSigningKey(),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
        };
    });

// Todo endpoint requiere sesión salvo los marcados con [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddAppRateLimits(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Detrás del proxy del hosting (Railway, nginx): IP real del cliente para el rate limit y esquema https.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference("/docs").AllowAnonymous();
}

// En desarrollo, y en producción con Database:MigrateOnStartup=true, la base se migra sola al arrancar.
if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

// En la imagen de producción la web compilada va en wwwroot y la sirve la misma API (un solo origen, sin CORS).
var servesWeb = File.Exists(Path.Combine(app.Environment.WebRootPath ?? "", "index.html"));
var webFiles = new StaticFileOptions
{
    // index.html y el service worker no se cachean para que las actualizaciones lleguen enseguida.
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name is "index.html" or "ngsw-worker.js" or "ngsw.json")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
    },
};
if (servesWeb)
{
    app.UseDefaultFiles();
    app.UseStaticFiles(webFiles);
}

// Explícito y después de los archivos estáticos: si no, el fallback a index.html se queda con todos los pedidos.
app.UseRouting();

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
if (servesWeb)
{
    // Rutas de Angular (/movimientos, /presupuesto…) devuelven index.html; lo que empieza con api/ sigue dando 404.
    app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html", webFiles).AllowAnonymous();
}

app.Run();

public partial class Program;
