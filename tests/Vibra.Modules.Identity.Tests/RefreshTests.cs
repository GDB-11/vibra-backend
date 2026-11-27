using System.Net;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Identity.Tests;

public sealed class RefreshTests : IClassFixture<PostgresFixture>
{
    private const string ActiveTokens =
        """SELECT count(*) FROM identity."RefreshToken" WHERE "RevokedAt" IS NULL""";

    private readonly PostgresFixture _postgres;

    public RefreshTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_links_the_replacement()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var first = await api.NewSessionAsync();

        var second = await IdentityApi.TokensAsync(await api.RefreshAsync(first.RefreshToken));

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(1L, await api.ScalarAsync<long>(ActiveTokens));
        Assert.Equal(1L, await api.ScalarAsync<long>(
            """
            SELECT count(*) FROM identity."RefreshToken" old
            JOIN identity."RefreshToken" new ON new."RefreshTokenId" = old."ReplacedByRefreshTokenId"
            WHERE old."TokenFamily" = new."TokenFamily" AND old."RevokedAt" IS NOT NULL
            """));
    }

    [Fact]
    public async Task Reusing_a_token_revokes_its_family_only()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();
        var first = await api.NewSessionAsync(email);
        var otherSession = await IdentityApi.TokensAsync(await api.LoginAsync(email));
        var second = await IdentityApi.TokensAsync(await api.RefreshAsync(first.RefreshToken));

        await Problem.AssertAsync(
            await api.RefreshAsync(first.RefreshToken), HttpStatusCode.Unauthorized, "invalid-refresh-token");

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.RefreshAsync(second.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.RefreshAsync(otherSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Simultaneous_use_of_one_token_succeeds_once_and_revokes_the_family()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var session = await api.NewSessionAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => api.RefreshAsync(session.RefreshToken)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(0L, await api.ScalarAsync<long>(ActiveTokens));
    }

    [Fact]
    public async Task Reuse_during_a_rotation_leaves_no_active_token()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        for (var round = 0; round < 20; round++)
        {
            var first = await api.NewSessionAsync();
            var second = await IdentityApi.TokensAsync(await api.RefreshAsync(first.RefreshToken));

            await Task.WhenAll(api.RefreshAsync(second.RefreshToken), api.RefreshAsync(first.RefreshToken));

            Assert.Equal(0L, await api.ScalarAsync<long>(ActiveTokens));
        }
    }

    [Fact]
    public async Task Expired_token_is_401()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var session = await api.NewSessionAsync();

        api.Clock.Advance(TimeSpan.FromDays(30));

        await Problem.AssertAsync(
            await api.RefreshAsync(session.RefreshToken), HttpStatusCode.Unauthorized, "invalid-refresh-token");
    }

    [Fact]
    public async Task Each_rotation_extends_the_session()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var session = await api.NewSessionAsync();

        for (var month = 0; month < 3; month++)
        {
            api.Clock.Advance(TimeSpan.FromDays(29));
            session = await IdentityApi.TokensAsync(await api.RefreshAsync(session.RefreshToken));
        }
    }

    [Fact]
    public async Task Unknown_token_is_401()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);

        await Problem.AssertAsync(
            await api.RefreshAsync("desconocido"), HttpStatusCode.Unauthorized, "invalid-refresh-token");
    }
}
