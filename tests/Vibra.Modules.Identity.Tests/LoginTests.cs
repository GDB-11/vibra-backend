using System.Net;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Identity.Tests;

public sealed class LoginTests : IClassFixture<PostgresFixture>
{
    private const string Wrong = "Incorrecta-123";

    private readonly PostgresFixture _postgres;

    public LoginTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Valid_credentials_return_tokens_ignoring_email_case()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        await api.NewSessionAsync("persona@example.com");

        var response = await api.LoginAsync("PERSONA@example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_look_the_same()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();
        await api.NewSessionAsync(email);

        var wrong = await Problem.AssertAsync(
            await api.LoginAsync(email, Wrong), HttpStatusCode.Unauthorized, "invalid-credentials");
        var unknown = await Problem.AssertAsync(
            await api.LoginAsync(IdentityApi.NewEmail()), HttpStatusCode.Unauthorized, "invalid-credentials");

        Assert.Equal(wrong.GetProperty("detail").GetString(), unknown.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Fifth_failure_locks_the_account_for_five_minutes()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();
        await api.NewSessionAsync(email);
        await FailAsync(api, email, 5);

        await Problem.AssertAsync(
            await api.LoginAsync(email), HttpStatusCode.Unauthorized, "invalid-credentials");

        api.Clock.Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(email)).StatusCode);

        api.Clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.OK, (await api.LoginAsync(email)).StatusCode);
    }

    [Fact]
    public async Task Successful_login_resets_the_failure_count()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();
        await api.NewSessionAsync(email);

        for (var round = 0; round < 2; round++)
        {
            await FailAsync(api, email, 4);
            Assert.Equal(HttpStatusCode.OK, (await api.LoginAsync(email)).StatusCode);
        }
    }

    [Fact]
    public async Task Simultaneous_failures_are_all_counted()
    {
        await using var api = await IdentityApi.StartAsync(_postgres);
        var email = IdentityApi.NewEmail();
        await api.NewSessionAsync(email);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => api.LoginAsync(email, Wrong)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(email)).StatusCode);
    }

    private static async Task FailAsync(IdentityApi api, string email, int times)
    {
        for (var attempt = 0; attempt < times; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(email, Wrong)).StatusCode);
    }
}
