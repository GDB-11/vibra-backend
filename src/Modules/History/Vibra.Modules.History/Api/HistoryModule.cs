using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.History.Api;

/// <summary>Punto de entrada del módulo History.</summary>
public static class HistoryModule
{
    /// <summary>Registra los servicios del módulo History.</summary>
    public static IServiceCollection AddHistoryModule(this IServiceCollection services) => services;
}
