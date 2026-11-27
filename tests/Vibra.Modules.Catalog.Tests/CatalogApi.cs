using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Catalog.Tests;

/// <summary>El host real sobre una base vacía migrada, con una sesión iniciada.</summary>
public sealed class CatalogApi : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _host;

    private CatalogApi(string connectionString, IReadOnlyDictionary<string, string> settings)
    {
        ConnectionString = connectionString;
        _host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("Jwt:SigningKey", "clave-solo-para-pruebas-0123456789abcdef");

            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        });
        Client = _host.CreateClient();
    }

    public HttpClient Client { get; }

    public string ConnectionString { get; }

    public static async Task<CatalogApi> StartAsync(
        PostgresFixture postgres, IReadOnlyDictionary<string, string>? settings = null)
    {
        var api = new CatalogApi(await postgres.CreateDatabaseAsync(), settings ?? new Dictionary<string, string>());
        await api.SignInAsync();

        return api;
    }

    public async Task ExecuteAsync(string sql, object? parameters = null)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, parameters);
    }

    /// <summary>Pide la ruta, comprueba que responde 200 y devuelve el JSON.</summary>
    public async Task<JsonElement> GetJsonAsync(string path)
    {
        using var response = await Client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"{path} {response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Recorre todas las páginas y devuelve los elementos en orden.</summary>
    public async Task<List<JsonElement>> WalkAsync(string path, int limit)
    {
        var items = new List<JsonElement>();
        string? cursor = null;

        // El tope evita un bucle infinito si un cursor no avanzara.
        for (var page = 0; page < 100; page++)
        {
            var json = await GetJsonAsync(
                $"{path}?limit={limit}" + (cursor is null ? string.Empty : $"&cursor={cursor}"));
            var pageItems = json.GetProperty("items").EnumerateArray().ToList();

            Assert.InRange(pageItems.Count, 0, limit);
            items.AddRange(pageItems);
            cursor = json.GetProperty("nextCursor").GetString();

            if (cursor is null)
                return items;

            Assert.Equal(limit, pageItems.Count);
        }

        throw new InvalidOperationException("La paginación no termina.");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.DisposeAsync();
    }

    private async Task SignInAsync()
    {
        using var response = await Client.PostAsJsonAsync("/v1/auth/register", new
        {
            email = $"persona-{Guid.NewGuid():N}@example.com",
            password = "Correcta-123",
            displayName = "Persona",
        });
        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
    }
}
