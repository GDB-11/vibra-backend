using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Identity.Api;

/// <summary>Punto de entrada del módulo Identity.</summary>
public static class IdentityModule
{
    /// <summary>Registra los servicios del módulo Identity.</summary>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services) => services;
}
