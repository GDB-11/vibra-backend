using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Billing.Api;

/// <summary>Punto de entrada del módulo Billing.</summary>
public static class BillingModule
{
    /// <summary>Registra los servicios del módulo Billing.</summary>
    public static IServiceCollection AddBillingModule(this IServiceCollection services) => services;
}
