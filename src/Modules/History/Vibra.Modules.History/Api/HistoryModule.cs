using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.History.Api;

/// <summary>Punto de entrada del módulo History.</summary>
public static class HistoryModule
{
    /// <summary>Migraciones del módulo History.</summary>
    public static ModuleMigrations Migrations { get; } = new("history", typeof(HistoryModule).Assembly);

    /// <summary>Registra los servicios del módulo History.</summary>
    public static IServiceCollection AddHistoryModule(this IServiceCollection services) => services;
}
