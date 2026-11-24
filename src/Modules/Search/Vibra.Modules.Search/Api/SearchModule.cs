using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Search.Api;

/// <summary>Punto de entrada del módulo Search.</summary>
public static class SearchModule
{
    /// <summary>Registra los servicios del módulo Search.</summary>
    public static IServiceCollection AddSearchModule(this IServiceCollection services) => services;
}
