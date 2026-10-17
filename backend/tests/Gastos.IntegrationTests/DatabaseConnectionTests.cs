using Gastos.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Gastos.IntegrationTests;

public sealed class DatabaseConnectionTests
{
    [Fact]
    public void Database_url_is_converted_to_an_npgsql_connection_string()
    {
        var cs = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.FromUrl("postgresql://postgres:p%40ss:w0rd@db.railway.internal:6543/railway?sslmode=require"));

        Assert.Equal(("db.railway.internal", 6543, "railway", "postgres", "p@ss:w0rd", SslMode.Require),
            (cs.Host, cs.Port, cs.Database, cs.Username, cs.Password, cs.SslMode));
    }

    [Fact]
    public void Database_url_wins_over_the_configured_connection_string()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=local",
            ["DATABASE_URL"] = "postgres://u:p@remote/prod",
        }).Build();

        var cs = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(config));

        Assert.Equal(("remote", 5432, "prod"), (cs.Host, cs.Port, cs.Database));
    }
}
