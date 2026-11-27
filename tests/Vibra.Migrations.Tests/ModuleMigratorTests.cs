using BindSharp;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Vibra.Api;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Migrations.Tests;

public sealed class ModuleMigratorTests : IClassFixture<PostgresFixture>
{
    private static readonly string[] ModuleNames =
        ["identity", "catalog", "streaming", "library", "history", "billing"];

    private static readonly ModuleMigrations Broken = new("broken", typeof(ModuleMigratorTests).Assembly);

    private readonly PostgresFixture _postgres;

    public ModuleMigratorTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private static int EmbeddedScripts(IEnumerable<ModuleMigrations> modules) =>
        modules.Sum(module => module.Assembly.GetManifestResourceNames()
            .Count(name => name.StartsWith(module.Name + "/", StringComparison.Ordinal)));

    [Fact]
    public async Task Empty_database_gets_every_module()
    {
        await using var database = await CreateDataSourceAsync();

        var result = Migrate(database, VibraMigrations.All);

        Assert.True(result.IsSuccess);
        Assert.Equal(EmbeddedScripts(VibraMigrations.All), result.Value);
        Assert.Equal(
            ModuleNames.Order(),
            await QueryAsync(database,
                "SELECT nspname FROM pg_namespace WHERE nspname = ANY(@Names) ORDER BY nspname",
                new { Names = ModuleNames }));
        Assert.Equal(["pg_trgm"], await QueryAsync(database,
            "SELECT extname FROM pg_extension WHERE extname = 'pg_trgm'"));

        foreach (var module in ModuleNames)
        {
            var journal = await JournalAsync(database, module);

            Assert.NotEmpty(journal);
            Assert.All(journal, script => Assert.StartsWith(module + "/", script));
        }
    }

    [Fact]
    public async Task Running_again_changes_nothing()
    {
        await using var database = await CreateDataSourceAsync();
        Assert.True(Migrate(database, VibraMigrations.All).IsSuccess);
        var before = await SnapshotAsync(database);

        var result = Migrate(database, VibraMigrations.All);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal(before, await SnapshotAsync(database));
    }

    // El criterio de aceptación de VIBR2-1033, tal cual.
    [Fact]
    public async Task Identity_and_catalog_apply_on_an_empty_database()
    {
        await using var database = await CreateDataSourceAsync();
        var modules = VibraMigrations.All.Take(2).ToList();

        var first = Migrate(database, modules);
        var second = Migrate(database, modules);

        Assert.Equal(["identity", "catalog"], modules.Select(module => module.Name));
        Assert.True(first.IsSuccess);
        Assert.Equal(EmbeddedScripts(modules), first.Value);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value);
    }

    // Comprueba que DbUp no altera los cuerpos $$ de las funciones.
    [Fact]
    public async Task Updated_at_trigger_works()
    {
        await using var database = await CreateDataSourceAsync();
        Assert.True(Migrate(database, VibraMigrations.All).IsSuccess);
        await using var connection = await database.OpenConnectionAsync();
        await connection.ExecuteAsync("""INSERT INTO catalog."Artist" ("Name") VALUES ('a')""");

        await connection.ExecuteAsync("""UPDATE catalog."Artist" SET "Name" = 'b'""");

        Assert.True(await connection.ExecuteScalarAsync<bool>(
            """SELECT "UpdatedAt" > "CreatedAt" FROM catalog."Artist" """));
    }

    [Fact]
    public async Task Failed_script_is_rolled_back_and_stops_later_modules()
    {
        await using var database = await CreateDataSourceAsync();

        var result = Migrate(database, [Broken, VibraMigrations.All[0]]);

        var error = Assert.IsType<MigrationFailedError>(result.Error);
        Assert.Equal("broken", error.Module);
        Assert.Equal("broken/0002_fails.sql", error.Script);
        Assert.NotNull(error.Cause);
        Assert.Equal(["broken/0001_ok.sql"], await JournalAsync(database, "broken"));
        Assert.Equal(["Applied"], await QueryAsync(database,
            "SELECT tablename FROM pg_tables WHERE tablename IN ('Applied', 'Half')"));
        Assert.Empty(await QueryAsync(database, "SELECT nspname FROM pg_namespace WHERE nspname = 'identity'"));
    }

    [Fact]
    public async Task Module_without_scripts_is_an_error()
    {
        await using var database = await CreateDataSourceAsync();

        var result = Migrate(database, [new ModuleMigrations("missing", Broken.Assembly)]);

        Assert.IsType<MissingMigrationsError>(result.Error);
    }

    [Fact]
    public async Task Unreachable_database_is_an_error_not_an_exception()
    {
        // Nada escucha en el puerto 1.
        await using var database = NpgsqlDataSource.Create("Host=127.0.0.1;Port=1;Username=x;Password=x;Timeout=2");

        var result = Migrate(database, VibraMigrations.All);

        var error = Assert.IsType<MigrationFailedError>(result.Error);
        Assert.Equal("identity", error.Module);
        Assert.NotNull(error.Cause);
    }

    private static Result<int, MigrationError> Migrate(
        NpgsqlDataSource database, IEnumerable<ModuleMigrations> modules) =>
        ModuleMigrator.Migrate(database, modules, NullLogger.Instance);

    private async Task<NpgsqlDataSource> CreateDataSourceAsync() =>
        NpgsqlDataSource.Create(await _postgres.CreateDatabaseAsync());

    private static async Task<List<string>> QueryAsync(
        NpgsqlDataSource database, string sql, object? parameters = null)
    {
        await using var connection = await database.OpenConnectionAsync();

        return (await connection.QueryAsync<string>(sql, parameters)).AsList();
    }

    // El nombre de la tabla sale de una lista fija de la prueba, no de datos externos.
    private static Task<List<string>> JournalAsync(NpgsqlDataSource database, string module) =>
        QueryAsync(database, $"SELECT scriptname FROM public.{module}_schemaversions ORDER BY schemaversionsid");

    /// <summary>Columnas de todas las tablas más las filas de los diarios, con su fecha.</summary>
    private static async Task<List<string>> SnapshotAsync(NpgsqlDataSource database)
    {
        var snapshot = await QueryAsync(database,
            """
            SELECT table_schema || '.' || table_name || '.' || column_name || ':' || data_type
            FROM information_schema.columns
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY 1
            """);

        foreach (var module in ModuleNames)
        {
            snapshot.AddRange(await QueryAsync(database,
                $"SELECT scriptname || '@' || applied::text FROM public.{module}_schemaversions ORDER BY 1"));
        }

        return snapshot;
    }
}
