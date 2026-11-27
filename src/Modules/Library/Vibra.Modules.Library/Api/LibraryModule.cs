using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Library.Api;

/// <summary>Punto de entrada del módulo Library.</summary>
public static class LibraryModule
{
    /// <summary>Migraciones del módulo Library.</summary>
    public static ModuleMigrations Migrations { get; } = new("library", typeof(LibraryModule).Assembly);

    /// <summary>Registra los servicios del módulo Library.</summary>
    public static IServiceCollection AddLibraryModule(this IServiceCollection services) => services;
}
