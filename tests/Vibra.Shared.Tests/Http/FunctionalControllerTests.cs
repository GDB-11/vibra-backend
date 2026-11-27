using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Vibra.Shared.Tests.Http;

public sealed class FunctionalControllerTests : IClassFixture<ProbeApiFactory>
{
    private readonly ProbeApiFactory _factory;
    private readonly HttpClient _client;

    public FunctionalControllerTests(ProbeApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Success_returns_200_with_the_value()
    {
        var response = await _client.GetAsync("/v1/test-probe/ok");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("fine", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Success_mapper_builds_created_location_from_the_route()
    {
        var response = await _client.PostAsJsonAsync("/v1/test-probe/items", new { id = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/v1/test-probe/items/7", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Expected_error_is_mapped_to_problem_details()
    {
        var problem = await ReadProblemAsync(
            await _client.GetAsync("/v1/test-probe/not-found"), HttpStatusCode.NotFound);

        Assert.Equal("urn:vibra:problem:probe-not-found", problem.GetProperty("type").GetString());
        Assert.Equal("El recurso no existe.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Data_error_is_generic_and_its_exception_is_only_logged()
    {
        var problem = await ReadProblemAsync(
            await _client.GetAsync("/v1/test-probe/data-error"), HttpStatusCode.InternalServerError);

        Assert.Equal("urn:vibra:problem:unexpected", problem.GetProperty("type").GetString());
        Assert.Contains(_factory.Logs.Entries, entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains(nameof(ProbeDataError), StringComparison.Ordinal)
            && entry.Exception?.Message == ProbeController.Secret);
    }

    [Fact]
    public async Task Unhandled_exception_is_a_generic_problem_even_in_development()
    {
        var problem = await ReadProblemAsync(
            await _client.GetAsync("/v1/test-probe/throws"), HttpStatusCode.InternalServerError);

        Assert.Equal("urn:vibra:problem:unexpected", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Unknown_route_is_a_problem_in_spanish()
    {
        var problem = await ReadProblemAsync(
            await _client.GetAsync("/v1/does-not-exist"), HttpStatusCode.NotFound);

        Assert.Equal("No encontrado", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{\"id\":\"not-a-guid\"}")]
    public async Task Invalid_json_does_not_reveal_type_names(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var problem = await ReadProblemAsync(
            await _client.PostAsync("/v1/test-probe/items", content), HttpStatusCode.BadRequest);

        Assert.DoesNotContain("System.", problem.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(ProbeBody), problem.GetRawText(), StringComparison.Ordinal);
        Assert.Equal("Solicitud no válida", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("not-a-guid", false)]
    [InlineData("9d2b8c1e-4f3a-4c55-9a2f-1d5e7b6a0c11", true)]
    public async Task Request_without_a_valid_authenticated_user_is_401(string? subject, bool unauthenticated)
    {
        await ReadProblemAsync(await GetMeAsync(subject, unauthenticated), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_user_id_reaches_the_operation()
    {
        var userId = Guid.NewGuid();

        var response = await GetMeAsync(userId.ToString(), unauthenticated: false);

        Assert.Equal(userId, await response.Content.ReadFromJsonAsync<Guid>());
    }

    private async Task<HttpResponseMessage> GetMeAsync(string? subject, bool unauthenticated)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/test-probe/me");

        if (subject is not null)
            request.Headers.Add(ProbeApiFactory.ClaimHeader, subject);

        if (unauthenticated)
            request.Headers.Add(ProbeApiFactory.AnonymousHeader, "true");

        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("SECRET", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);

        var problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));

        return problem;
    }
}
