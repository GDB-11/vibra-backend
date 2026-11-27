using System.Net;
using System.Text.Json;

namespace Vibra.Modules.Identity.Tests;

internal static class Problem
{
    public static async Task<JsonElement> AssertAsync(
        HttpResponseMessage response, HttpStatusCode status, string? type = null)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(status == response.StatusCode, $"{response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonDocument.Parse(body).RootElement.Clone();

        if (type is not null)
            Assert.Equal("urn:vibra:problem:" + type, problem.GetProperty("type").GetString());

        return problem;
    }
}
