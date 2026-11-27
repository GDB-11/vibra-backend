using System.Reflection;
using Microsoft.OpenApi;
using Npgsql;
using Scalar.AspNetCore;
using Vibra.Api;
using Vibra.Modules.Billing.Api;
using Vibra.Modules.Catalog.Api;
using Vibra.Modules.History.Api;
using Vibra.Modules.Identity.Api;
using Vibra.Modules.Library.Api;
using Vibra.Modules.Streaming.Api;
using Vibra.Shared.Api;
using Vibra.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

// El generador de OpenAPI también ejecuta este código y no tiene base de datos ni secretos.
var generatingOpenApi = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.Services.AddSingleton(TimeProvider.System);

// Se crea al primer uso: arrancar y exportar OpenAPI no requieren secretos.
// La sesión de la base de datos va siempre en UTC (decisión 017).
builder.Services.AddSingleton(services => NpgsqlDataSource.Create(
    new NpgsqlConnectionStringBuilder(
        services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Falta la configuración ConnectionStrings:Postgres."))
    {
        Timezone = "UTC",
    }.ConnectionString));

builder.Services.AddVibraControllers();

// La versión 3.1 es la que admite el generador de Swift (decisión 005).
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo { Title = "Vibra API", Version = "0.1.0" };
        return Task.CompletedTask;
    });
});

builder.Services
    .AddIdentityModule(validateOnStart: !generatingOpenApi)
    .AddCatalogModule()
    .AddLibraryModule()
    .AddStreamingModule()
    .AddHistoryModule()
    .AddBillingModule();

var app = builder.Build();

// En proceso y antes de atender solicitudes (decisión 002). Si falta la opción, no se migra.
if (!generatingOpenApi && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    var migration = ModuleMigrator.Migrate(
        app.Services.GetRequiredService<NpgsqlDataSource>(), VibraMigrations.All, app.Logger);

    if (migration.IsFailure)
        throw new InvalidOperationException(migration.Error.Message, migration.Error.Cause);
}

app.UseVibraErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Solo en desarrollo: documento OpenAPI y Scalar (/scalar) para probar la API.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Vibra API")
        .WithTheme(ScalarTheme.DeepSpace)
        .ForceDarkMode()
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
        .DisableAgent()
        .DisableMcp()
        .DisableTelemetry());
}

app.Run();
