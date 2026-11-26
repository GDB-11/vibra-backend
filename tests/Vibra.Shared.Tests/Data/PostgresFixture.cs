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
            DROP TABLE IF EXISTS "Item", "Owner";
            CREATE TABLE "Owner" ("OwnerId" int CONSTRAINT pk_owner PRIMARY KEY);
            CREATE TABLE "Item" (
                "ItemId" int CONSTRAINT pk_item PRIMARY KEY,
                "OwnerId" int NOT NULL CONSTRAINT fk_item_owner_id REFERENCES "Owner" ("OwnerId"),
                "DisplayName" text NOT NULL);
            INSERT INTO "Owner" VALUES (1);
            """);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}
