using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Identity.Api;

/// <summary>Punto de entrada del módulo Identity.</summary>
public static class IdentityModule
{
    /// <summary>Migraciones del módulo Identity.</summary>
    public static ModuleMigrations Migrations { get; } = new("identity", typeof(IdentityModule).Assembly);

    /// <summary>Registra los servicios del módulo Identity.</summary>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services) => services;
}
