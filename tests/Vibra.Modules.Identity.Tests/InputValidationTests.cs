using System.Net;
using System.Text;
using System.Text.Json;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Identity.Tests;

/// <summary>La entrada no válida se rechaza con 400, con mensajes por campo, y no llega a la base.</summary>
public sealed class InputValidationTests : IClassFixture<PostgresFixture>
{
    private const string Email = "\"email\":\"a@example.com\"";
    private const string Pass = "\"password\":\"Correcta-123\"";
    private const string Name = "\"displayName\":\"Ana\"";

    private readonly PostgresFixture _postgres;

    public InputValidationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Theory]
    [InlineData("a@b")]
    [InlineData("a@localhost")]
    [InlineData("a@example.")]
    [InlineData("a@.example.com")]
    [InlineData("a@example..com")]
    [InlineData("a@-..-")]
    [InlineData("a@-example.com")]
    [InlineData("a@example.c")]
    [InlineData("a@192.168.0.1")]
    [InlineData("a@exa_mple.com")]
    [InlineData("a b@example.com")]
    [InlineData("a​@example.com")]
    [InlineData("Nombre <a@example.com>")]
    [InlineData("sin-arroba")]
    public async Task Invalid_email_is_rejected(string email) =>
        await AssertRejectedAsync("register", Register(email, "Ana"), "email", "El correo no es válido.");

    [Theory]
    [InlineData("​​", "El nombre contiene caracteres no permitidos.")]
    [InlineData("Ana‮gpj", "El nombre contiene caracteres no permitidos.")]
    [InlineData("A\u0000", "El nombre contiene caracteres no permitidos.")]
    [InlineData("Ana\nLuz", "El nombre contiene caracteres no permitidos.")]
    [InlineData("ㅤ", "El nombre debe incluir al menos un carácter visible.")]
    [InlineData("‍‍", "El nombre debe incluir al menos un carácter visible.")]
    [InlineData("   ", "El nombre es obligatorio.")]
    public async Task Invalid_display_name_is_rejected(string name, string message) =>
        await AssertRejectedAsync("register", Register("a@example.com", name), "displayName", message);

    [Theory]
    [InlineData("José Ñandú", "jose@correo.com.pe")]
    [InlineData("李小龙", "li@例え.jp")]
    [InlineData("محمد", "m.ali+vibra@sub.example.org")]
    [InlineData("👩‍💻", "dev@example.io")]
    [InlineData("O'Brien-Smith 3.º", "  ob@example.com  ")]
    public async Task Legitimate_input_is_accepted(string name, string email)
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        var response = await api.RegisterAsync(email, displayName: name);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task All_field_errors_are_returned_together()
    {
        var errors = await AssertRejectedAsync(
            "register", """{"email":"a@b","displayName":"  "}""", "email", "El correo no es válido.");

        Assert.Equal(["displayName", "email", "password"], errors.EnumerateObject().Select(e => e.Name).Order());
    }

    [Theory]
    [InlineData("register", "email", "El correo es obligatorio.")]
    [InlineData("register", "password", "La contraseña es obligatoria.")]
    [InlineData("register", "displayName", "El nombre es obligatorio.")]
    [InlineData("login", "email", "El correo es obligatorio.")]
    [InlineData("login", "password", "La contraseña es obligatoria.")]
    [InlineData("refresh", "refreshToken", "El token de renovación es obligatorio.")]
    public async Task Missing_or_null_field_has_a_spanish_message(string endpoint, string field, string message)
    {
        await AssertRejectedAsync(endpoint, "{}", field, message);
        await AssertRejectedAsync(endpoint, $$"""{"{{field}}":null}""", field, message);
    }

    [Theory]
    [InlineData("login", "email", 255, "El correo no puede superar los 254 caracteres.")]
    [InlineData("login", "password", 129, "La contraseña no puede superar los 128 caracteres.")]
    [InlineData("register", "password", 129, "La contraseña no puede superar los 128 caracteres.")]
    [InlineData("register", "displayName", 101, "El nombre no puede superar los 100 caracteres.")]
    [InlineData("refresh", "refreshToken", 129, "El token de renovación no es válido.")]
    public async Task Oversized_field_is_rejected(string endpoint, string field, int length, string message) =>
        await AssertRejectedAsync(endpoint, $$"""{"{{field}}":"{{new string('x', length)}}"}""", field, message);

    [Theory]
    [InlineData($$"""{{{Email}},{{Pass}},{{Name}},"isAdmin":true}""", "isAdmin")]
    [InlineData($$"""{"email":"mal",{{Email}},{{Pass}},{{Name}}}""", "email")]
    [InlineData($$"""{"email":5,{{Pass}},{{Name}}}""", "email")]
    public async Task Unknown_duplicate_or_mistyped_field_is_rejected(string json, string field) =>
        await AssertRejectedAsync("register", json, field, "El campo no está permitido o su valor no es válido.");

    [Theory]
    [InlineData("{ no es json", null)]
    [InlineData("[]", null)]
    [InlineData("", "El cuerpo de la solicitud es obligatorio.")]
    [InlineData("null", "El cuerpo de la solicitud es obligatorio.")]
    public async Task Unreadable_body_is_rejected_in_spanish(string json, string? message)
    {
        await AssertRejectedAsync("register", json, "body", message ?? "El cuerpo no es un JSON válido.");
    }

    private static string Register(string email, string name) =>
        JsonSerializer.Serialize(new { email, password = IdentityApi.Password, displayName = name });

    private async Task<JsonElement> AssertRejectedAsync(string endpoint, string json, string field, string message)
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var problem = await Problem.AssertAsync(
            await api.Client.PostAsync($"/v1/auth/{endpoint}", content), HttpStatusCode.BadRequest, "validation");

        Assert.Equal("Solicitud no válida", problem.GetProperty("title").GetString());
        Assert.Equal("Revisa los campos indicados.", problem.GetProperty("detail").GetString());

        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out var messages), problem.GetRawText());
        Assert.Contains(message, messages.EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(0L, await api.ScalarAsync<long>("""SELECT count(*) FROM identity."User" """));

        return errors;
    }
}
