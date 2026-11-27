using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BindSharp;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vibra.Migrations.Tests;
using Vibra.Modules.Identity.Api.ErrorMappers;
using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Shared.Api;

namespace Vibra.Modules.Identity.Tests;

/// <summary>Reloj que solo avanza cuando la prueba lo pide.</summary>
public sealed class FakeClock : TimeProvider
{
    // Lejos del reloj real: si algo usara el reloj del sistema, las pruebas fallarían.
    private DateTimeOffset _now = new(2001, 3, 4, 5, 6, 7, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan time) => _now += time;
}

/// <summary>El host real sobre una base vacía migrada.</summary>
public sealed class IdentityApi : IAsyncDisposable
{
    public const string Password = "Correcta-123";
    public const string SigningKey = "clave-solo-para-pruebas-0123456789abcdef";

    private readonly WebApplicationFactory<Program> _host;

    private IdentityApi(string connectionString, bool migrate)
    {
        ConnectionString = connectionString;
        _host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", migrate ? "true" : "false");
            builder.UseSetting("Jwt:SigningKey", SigningKey);
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<TimeProvider>(Clock);
                services.AddControllers().AddApplicationPart(typeof(WhoAmIController).Assembly);
            });
        });
        Client = _host.CreateClient();
    }

    public FakeClock Clock { get; } = new();

    public HttpClient Client { get; }

    public string ConnectionString { get; }

    public static async Task<IdentityApi> StartAsync(PostgresFixture postgres, bool migrate = true) =>
        new(await postgres.CreateDatabaseAsync(), migrate);

    public static string NewEmail() => $"persona-{Guid.NewGuid():N}@example.com";

    public Task<HttpResponseMessage> RegisterAsync(
        string email, string password = Password, string displayName = "Persona") =>
        Client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, password, displayName));

    public Task<HttpResponseMessage> LoginAsync(string email, string password = Password) =>
        Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, password));

    public Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Client.PostAsJsonAsync("/v1/auth/refresh", new RefreshRequest(refreshToken));

    public async Task<HttpResponseMessage> WhoAmIAsync(string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/test-whoami");

        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await Client.SendAsync(request);
    }

    /// <summary>Registra una cuenta nueva y devuelve sus tokens.</summary>
    public async Task<AuthTokens> NewSessionAsync(string? email = null) =>
        await TokensAsync(await RegisterAsync(email ?? NewEmail()));

    public static async Task<AuthTokens> TokensAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    public async Task<TValue> ScalarAsync<TValue>(string sql, object? parameters = null)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);

        return (await connection.ExecuteScalarAsync<TValue>(sql, parameters))!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.DisposeAsync();
    }
}

/// <summary>Endpoint protegido solo de pruebas: valida el JWT igual que lo hará un módulo.</summary>
[Route("v1/test-whoami")]
[Authorize]
public sealed class WhoAmIController : FunctionalController
{
    private readonly IAuthErrorMapper _errorMapper;

    public WhoAmIController(IAuthErrorMapper errorMapper, ILogger<FunctionalController> logger)
        : base(logger)
    {
        _errorMapper = errorMapper;
    }

    [HttpGet]
    public Task<IActionResult> Get() =>
        ExecuteAuthenticatedAsync(
            userCode => Task.FromResult(Result<Guid, IdentityModuleError>.Success(userCode)), _errorMapper, nameof(Get));
}
