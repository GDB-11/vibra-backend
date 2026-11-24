using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Catalog.Api;

/// <summary>Punto de entrada del módulo Catalog.</summary>
public static class CatalogModule
{
    /// <summary>Registra los servicios del módulo Catalog.</summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services) => services;
}
