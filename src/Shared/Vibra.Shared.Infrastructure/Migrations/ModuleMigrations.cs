using System.Reflection;

namespace Vibra.Shared.Infrastructure.Migrations;

/// <summary>
/// Migraciones de un módulo: los recursos <c>{Name}/*.sql</c> incrustados en <paramref name="Assembly"/>.
/// </summary>
/// <param name="Name">Nombre del módulo en minúsculas; también da nombre a su diario.</param>
public sealed record ModuleMigrations(string Name, Assembly Assembly);
