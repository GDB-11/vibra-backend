using Vibra.Modules.Billing.Api;
using Vibra.Modules.Catalog.Api;
using Vibra.Modules.History.Api;
using Vibra.Modules.Identity.Api;
using Vibra.Modules.Library.Api;
using Vibra.Modules.Streaming.Api;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Api;

/// <summary>Migraciones de toda la aplicación, para el host y las pruebas.</summary>
public static class VibraMigrations
{
    /// <summary>
    /// El orden es obligatorio por las claves foráneas entre esquemas (decisión 017):
    /// Identity, Catalog, Streaming, Library, History y Billing.
    /// </summary>
    public static IReadOnlyList<ModuleMigrations> All { get; } =
    [
        IdentityModule.Migrations,
        CatalogModule.Migrations,
        StreamingModule.Migrations,
        LibraryModule.Migrations,
        HistoryModule.Migrations,
        BillingModule.Migrations,
    ];
}
