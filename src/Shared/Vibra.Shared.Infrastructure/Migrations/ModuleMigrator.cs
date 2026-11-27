using BindSharp;
using DbUp;
using DbUp.Engine;
using DbUp.Postgresql;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Vibra.Shared.Infrastructure.Migrations;

/// <summary>
/// Aplica con DbUp las migraciones de cada módulo, con un diario por módulo
/// (<c>public.{módulo}_schemaversions</c>). La usan el host y las pruebas (decisión 002).
/// DbUp no bloquea entre procesos: solo una instancia debe migrar a la vez.
/// </summary>
public static class ModuleMigrator
{
    private const string JournalSchema = "public";

    /// <summary>
    /// Migra los módulos en el orden recibido y se detiene en el primer fallo.
    /// Devuelve cuántos scripts se aplicaron.
    /// </summary>
    /// <param name="dataSource">Debe abrir las sesiones en UTC (decisión 017).</param>
    public static Result<int, MigrationError> Migrate(
        NpgsqlDataSource dataSource, IEnumerable<ModuleMigrations> modules, ILogger logger)
    {
        var applied = 0;

        foreach (var module in modules)
        {
            var result = MigrateModule(dataSource, module, logger);

            if (result.IsFailure)
                return result.Error;

            applied += result.Value;
        }

        return applied;
    }

    private static Result<int, MigrationError> MigrateModule(
        NpgsqlDataSource dataSource, ModuleMigrations module, ILogger logger)
    {
        var prefix = module.Name + "/";

        bool BelongsToModule(string resource) =>
            resource.StartsWith(prefix, StringComparison.Ordinal)
            && resource.EndsWith(".sql", StringComparison.Ordinal);

        if (!module.Assembly.GetManifestResourceNames().Any(BelongsToModule))
            return new MissingMigrationsError(module.Name);

        return Result.Try<DatabaseUpgradeResult, MigrationError>(
                () => DeployChanges.To
                    .PostgresqlDatabase(new PostgresqlConnectionManager(dataSource))
                    .WithScriptsEmbeddedInAssembly(module.Assembly, BelongsToModule)
                    .JournalToPostgresqlTable(JournalSchema, $"{module.Name}_schemaversions")
                    // Sin esto, un script que falla a medias deja objetos creados y sin registrar.
                    .WithTransactionPerScript()
                    // El SQL usa cuerpos $$ y no hay variables de DbUp que sustituir.
                    .WithVariablesDisabled()
                    .LogTo(logger)
                    .Build()
                    .PerformUpgrade(),
                exception => new MigrationFailedError(module.Name, null, exception))
            .Bind(upgrade => upgrade.Successful
                ? Result<int, MigrationError>.Success(upgrade.Scripts.Count())
                : new MigrationFailedError(module.Name, upgrade.ErrorScript?.Name, upgrade.Error));
    }
}
