using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Vibra.Shared.Tests.Data;

/// <summary>PostgreSQL real con Testcontainers; la misma imagen que usa vibra-db.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // La espera por defecto usa "exec" (pg_isready), que falla en Podman sin root en este entorno
    // (crun: oom_score_adj). Conectarse desde el host no depende de "exec".
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("docker.io/library/postgres:18.7")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilDatabaseIsAvailable(NpgsqlFactory.Instance))
            .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
    }

    public async Task ResetAsync()
    {
        await using var command = DataSource.CreateCommand(
            """
            DROP TABLE IF EXISTS items, owners;
            CREATE TABLE owners (id int PRIMARY KEY);
            CREATE TABLE items (
                id int CONSTRAINT items_pkey PRIMARY KEY,
                owner_id int NOT NULL CONSTRAINT items_owner_fk REFERENCES owners (id),
                display_name text NOT NULL);
            INSERT INTO owners VALUES (1);
            """);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}
