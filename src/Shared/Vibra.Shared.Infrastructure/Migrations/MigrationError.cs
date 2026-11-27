using Vibra.Shared.Domain;

namespace Vibra.Shared.Infrastructure.Migrations;

/// <summary>Errores al migrar la base de datos.</summary>
public closed record MigrationError(string Module, string Message, Exception? Cause) : IModuleError;

/// <summary>El módulo no tiene scripts incrustados: la sincronización o el nombre del recurso fallan.</summary>
public sealed record MissingMigrationsError(string Module)
    : MigrationError(Module, $"El módulo '{Module}' no tiene migraciones incrustadas.", null);

/// <summary>Falló un script o, si <paramref name="Script"/> es <c>null</c>, la preparación (p. ej. la conexión).</summary>
public sealed record MigrationFailedError(string Module, string? Script, Exception Exception)
    : MigrationError(
        Module,
        Script is null
            ? $"No se pudo migrar el módulo '{Module}'."
            : $"Falló la migración '{Script}' del módulo '{Module}'.",
        Exception);
