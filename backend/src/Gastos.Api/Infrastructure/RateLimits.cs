using System.Threading.RateLimiting;

namespace Gastos.Api.Infrastructure;

public static class RateLimits
{
    public const string Auth = "auth";

    /// <summary>Limita los intentos de login/registro por IP para frenar fuerza bruta.</summary>
    public static IServiceCollection AddAppRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue("RateLimits:AuthPerMinute", 10);
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Auth, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });
    }
}
