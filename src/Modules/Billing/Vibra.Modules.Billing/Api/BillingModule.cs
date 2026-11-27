using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Billing.Api;

/// <summary>Punto de entrada del módulo Billing.</summary>
public static class BillingModule
{
    /// <summary>Migraciones del módulo Billing.</summary>
    public static ModuleMigrations Migrations { get; } = new("billing", typeof(BillingModule).Assembly);

    /// <summary>Registra los servicios del módulo Billing.</summary>
    public static IServiceCollection AddBillingModule(this IServiceCollection services) => services;
}
