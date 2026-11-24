using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Library.Api;

/// <summary>Punto de entrada del módulo Library.</summary>
public static class LibraryModule
{
    /// <summary>Registra los servicios del módulo Library.</summary>
    public static IServiceCollection AddLibraryModule(this IServiceCollection services) => services;
}
