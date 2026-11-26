using Microsoft.OpenApi;
using Npgsql;
using Vibra.Modules.Billing.Api;
using Vibra.Modules.Catalog.Api;
using Vibra.Modules.History.Api;
using Vibra.Modules.Identity.Api;
using Vibra.Modules.Library.Api;
using Vibra.Modules.Streaming.Api;
using Vibra.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

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
    .AddIdentityModule()
    .AddCatalogModule()
    .AddLibraryModule()
    .AddStreamingModule()
    .AddHistoryModule()
    .AddBillingModule();

var app = builder.Build();

app.UseVibraErrorHandling();
app.MapControllers();

app.Run();
