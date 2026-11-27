using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Catalog.Api;

/// <summary>Punto de entrada del módulo Catalog.</summary>
public static class CatalogModule
{
    /// <summary>Migraciones del módulo Catalog.</summary>
    public static ModuleMigrations Migrations { get; } = new("catalog", typeof(CatalogModule).Assembly);

    /// <summary>Registra los servicios del módulo Catalog.</summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services) => services;
}
