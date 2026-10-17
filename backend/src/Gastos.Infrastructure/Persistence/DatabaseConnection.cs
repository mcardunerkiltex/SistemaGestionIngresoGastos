using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Gastos.Infrastructure.Persistence;

public static class DatabaseConnection
{
    /// <summary>
    /// Cadena de conexión a Postgres. Si existe DATABASE_URL (formato postgres://usuario:clave@host:puerto/base,
    /// el que dan Railway, Render y Heroku) tiene prioridad sobre ConnectionStrings:Default.
    /// </summary>
    public static string Resolve(IConfiguration configuration)
    {
        var url = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(url)) return FromUrl(url);
        return configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default o DATABASE_URL.");
    }

    public static string FromUrl(string url)
    {
        var uri = new Uri(url);
        if (uri.Scheme is not ("postgres" or "postgresql"))
            throw new InvalidOperationException("DATABASE_URL tiene que empezar con postgres:// o postgresql://.");

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };

        // Respeta ?sslmode=require y similares si vienen en la URL.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var (key, value) = (pair.Split('=', 2)[0], pair.Contains('=') ? pair.Split('=', 2)[1] : "");
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = Enum.Parse<SslMode>(Uri.UnescapeDataString(value).Replace("-", ""), ignoreCase: true);
        }
        return builder.ConnectionString;
    }
}
