using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Vibra.Migrations.Tests;

/// <summary>Un PostgreSQL real; cada prueba usa su propia base de datos vacía.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // La espera por defecto usa "exec" (pg_isready), que falla en Podman sin root en este entorno
    // (crun: oom_score_adj). Conectarse desde el host no depende de "exec".
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("docker.io/library/postgres:18.7")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilDatabaseIsAvailable(NpgsqlFactory.Instance))
            .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Crea una base vacía y devuelve su cadena de conexión.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";

        await using var server = NpgsqlDataSource.Create(_container.GetConnectionString());
        await using var command = server.CreateCommand($"CREATE DATABASE {name}");
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = name,
            Timezone = "UTC",
        }.ConnectionString;
    }
}
