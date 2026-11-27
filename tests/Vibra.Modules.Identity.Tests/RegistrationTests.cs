using System.Net;
using System.Net.Http.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Identity.Tests;

public sealed class RegistrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public RegistrationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Register_returns_201_with_tokens_that_must_not_be_cached()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        var response = await api.RegisterAsync(IdentityApi.NewEmail());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);

        var tokens = await IdentityApi.TokensAsync(response);
        Assert.Equal(api.Clock.GetUtcNow().AddMinutes(15), tokens.AccessTokenExpiresAt);
        Assert.Equal(api.Clock.GetUtcNow().AddDays(30), tokens.RefreshTokenExpiresAt);

        var userCode = await api.ScalarAsync<Guid>("""SELECT "UserCode" FROM identity."User" """);
        Assert.Equal(userCode.ToString(), new JsonWebToken(tokens.AccessToken).Subject);
    }

    [Fact]
    public async Task Password_and_refresh_token_are_stored_only_as_hashes()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        var tokens = await api.NewSessionAsync();

        Assert.NotEqual(IdentityApi.Password,
            await api.ScalarAsync<string>("""SELECT "PasswordHash" FROM identity."User" """));
        Assert.NotEqual(tokens.RefreshToken,
            await api.ScalarAsync<string>("""SELECT "TokenHash" FROM identity."RefreshToken" """));
    }

    [Fact]
    public async Task Duplicate_email_ignoring_case_is_409()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        await api.NewSessionAsync("persona@example.com");

        await Problem.AssertAsync(
            await api.RegisterAsync("  PERSONA@Example.com "), HttpStatusCode.Conflict, "duplicate-email");
    }

    [Fact]
    public async Task Simultaneous_registrations_create_one_account()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => api.RegisterAsync(email)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Theory]
    [InlineData("Ab1-x", "al menos 6")]
    [InlineData("abcdef1-", "mayúscula")]
    [InlineData("ABCDEF1-", "minúscula")]
    [InlineData("Abcdefg-", "número")]
    [InlineData("Abcdefg1", "símbolo")]
    public async Task Weak_password_is_400_and_explains_the_rule(string password, string rule)
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        var problem = await Problem.AssertAsync(
            await api.RegisterAsync(IdentityApi.NewEmail(), password), HttpStatusCode.BadRequest, "validation");

        Assert.Contains(problem.GetProperty("errors").GetProperty("password").EnumerateArray(),
            message => message.GetString()!.Contains(rule, StringComparison.Ordinal));
        Assert.Equal(0L, await api.ScalarAsync<long>("""SELECT count(*) FROM identity."User" """));
    }

    [Theory]
    [InlineData("sin-arroba", IdentityApi.Password, "Persona")]
    [InlineData("Nombre <a@example.com>", IdentityApi.Password, "Persona")]
    [InlineData("a@example.com", IdentityApi.Password, "   ")]
    public async Task Invalid_input_is_400(string email, string password, string displayName)
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        await Problem.AssertAsync(
            await api.RegisterAsync(email, password, displayName), HttpStatusCode.BadRequest, "validation");
    }

    [Fact]
    public async Task Oversized_password_is_400()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        await Problem.AssertAsync(
            await api.RegisterAsync(IdentityApi.NewEmail(), "Aa1-" + new string('x', 125)),
            HttpStatusCode.BadRequest, "validation");
    }

    [Theory]
    [InlineData("register")]
    [InlineData("login")]
    [InlineData("refresh")]
    public async Task Missing_fields_are_400(string endpoint)
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        await Problem.AssertAsync(
            await api.Client.PostAsJsonAsync($"/v1/auth/{endpoint}", new { }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Data_access_failure_is_a_generic_500()
    {
        // Sin migrar: las tablas no existen.
        await using var api = await IdentityApi.StartAsync(_postgres, migrate: false);

        foreach (var response in new[]
                 {
                     await api.RegisterAsync(IdentityApi.NewEmail()),
                     await api.LoginAsync(IdentityApi.NewEmail()),
                     await api.RefreshAsync("token"),
                 })
        {
            var problem = await Problem.AssertAsync(response, HttpStatusCode.InternalServerError, "unexpected");
            Assert.DoesNotContain("identity", problem.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
