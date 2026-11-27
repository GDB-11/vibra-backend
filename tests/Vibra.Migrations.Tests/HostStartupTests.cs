using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Vibra.Migrations.Tests;

public sealed class HostStartupTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public HostStartupTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    // No fija "Database:MigrateOnStartup": comprueba el valor de appsettings.json.
    [Fact]
    public async Task Host_migrates_the_database_on_startup()
    {
        var connectionString = await _postgres.CreateDatabaseAsync();
        await using var host = CreateHost(connectionString);

        using var client = host.CreateClient();

        await using var connection = new NpgsqlConnection(connectionString);
        Assert.Equal(6, await connection.ExecuteScalarAsync<long>(
            """
            SELECT count(*) FROM pg_tables
            WHERE schemaname = 'public' AND tablename LIKE '%\_schemaversions'
            """));
    }

    [Fact]
    public async Task Host_does_not_start_if_migration_fails()
    {
        await using var host = CreateHost("Host=127.0.0.1;Port=1;Username=x;Password=x;Timeout=2");

        var exception = Assert.Throws<InvalidOperationException>(() => host.CreateClient());

        Assert.Equal("No se pudo migrar el módulo 'identity'.", exception.Message);
    }

    private static WebApplicationFactory<Program> CreateHost(string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Sin los secretos de usuario locales, que solo se cargan en Development.
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Jwt:SigningKey", "clave-solo-para-pruebas-0123456789abcdef");
        });
}
