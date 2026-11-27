using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Identity.Tests;

public sealed class AccessTokenTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public AccessTokenTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Issued_token_identifies_the_user()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var session = await api.NewSessionAsync();

        var response = await api.WhoAmIAsync(session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            Guid.Parse(new JsonWebToken(session.AccessToken).Subject),
            await response.Content.ReadFromJsonAsync<Guid>());
    }

    [Fact]
    public async Task Token_expires_with_the_injected_clock()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var session = await api.NewSessionAsync();

        api.Clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await api.WhoAmIAsync(session.AccessToken)).StatusCode);

        api.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.WhoAmIAsync(session.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Missing_or_altered_token_is_401()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var mine = (await api.NewSessionAsync()).AccessToken.Split('.');
        var other = (await api.NewSessionAsync()).AccessToken.Split('.');

        await Problem.AssertAsync(await api.WhoAmIAsync(null), HttpStatusCode.Unauthorized);
        // El contenido de otro usuario con mi firma.
        await Problem.AssertAsync(
            await api.WhoAmIAsync($"{mine[0]}.{other[1]}.{mine[2]}"), HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("demasiado-corta")]
    public async Task Host_does_not_start_without_a_strong_signing_key(string key)
    {
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Jwt:SigningKey", key);
        });

        Assert.Throws<OptionsValidationException>(() => host.CreateClient());
    }
}
