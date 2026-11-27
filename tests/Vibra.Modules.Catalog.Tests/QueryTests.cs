using System.Net;
using System.Text.Json;
using Vibra.Shared.Domain;

namespace Vibra.Modules.Catalog.Tests;

public sealed class QueryTests : IClassFixture<SampleCatalogFixture>
{
    private readonly CatalogApi _api;

    public QueryTests(SampleCatalogFixture fixture)
    {
        _api = fixture.Api;
    }

    private async Task<List<string>> InvalidFieldsAsync(string path)
    {
        using var response = await _api.Client.GetAsync(path);
        var problem = await Problem.AssertAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.True(problem.TryGetProperty("traceId", out _));

        return [.. problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order()];
    }

    public static TheoryData<string> ListPaths => new()
    {
        "/v1/artists",
        $"/v1/artists/{SampleCatalog.Band}/albums",
        "/v1/albums",
        $"/v1/albums/{SampleCatalog.DoubleAlbum}/tracks",
        "/v1/tracks",
    };

    [Theory]
    [InlineData("1")]
    [InlineData("50")]
    public async Task Limit_in_range_is_accepted(string limit) =>
        await _api.GetJsonAsync($"/v1/tracks?limit={limit}");

    [Theory]
    [InlineData("0")]
    [InlineData("51")]
    [InlineData("-1")]
    [InlineData("%2B5")]
    [InlineData("%205")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("99999999999999999999")]
    public async Task Limit_out_of_range_or_not_a_number_is_rejected(string limit) =>
        Assert.Equal(["limit"], await InvalidFieldsAsync($"/v1/tracks?limit={limit}"));

    [Theory]
    [MemberData(nameof(ListPaths))]
    public async Task Every_list_validates_its_query(string path) =>
        Assert.Equal(["cursor", "limit", "page"], await InvalidFieldsAsync($"{path}?limit=0&cursor=x&page=2"));

    [Theory]
    [InlineData("limit=1&limit=2", "limit")]
    // ASP.NET Core agrupa ambos nombres y conserva el último.
    [InlineData("limit=1&LIMIT=2", "LIMIT")]
    [InlineData("Limit=1", "Limit")]
    [InlineData("limt=5", "limt")]
    [InlineData("cursor=", "cursor")]
    public async Task Repeated_unknown_or_empty_parameters_are_rejected(string query, string field) =>
        Assert.Equal([field], await InvalidFieldsAsync($"/v1/tracks?{query}"));

    [Theory]
    [InlineData("artists")]
    [InlineData("albums")]
    [InlineData("tracks")]
    public async Task Detail_routes_accept_no_parameters(string resource) =>
        Assert.Equal(["limit"], await InvalidFieldsAsync($"/v1/{resource}/{Guid.NewGuid()}?limit=5"));

    [Fact]
    public async Task Cursor_of_another_resource_is_rejected()
    {
        var cursor = PageCursor.Encode(CursorKind.Album, SampleCatalog.DoubleAlbum);

        Assert.Equal(["cursor"], await InvalidFieldsAsync($"/v1/tracks?cursor={cursor}"));
    }

    // Bien formados, pero no corresponden a un elemento de la lista pedida.
    public static TheoryData<string, CursorKind, Guid> ForeignCursors => new()
    {
        { "/v1/artists", CursorKind.Artist, Guid.Empty },
        { "/v1/albums", CursorKind.Album, Guid.Empty },
        { "/v1/tracks", CursorKind.Track, Guid.Empty },
        // Un álbum de "Banda" no pertenece a la lista de la invitada.
        { $"/v1/artists/{SampleCatalog.Guest}/albums", CursorKind.Album, SampleCatalog.DoubleAlbum },
        // "Apertura" no está en el primer "Eco".
        { $"/v1/albums/{SampleCatalog.FirstEcho}/tracks", CursorKind.Track, SampleCatalog.Opening },
    };

    [Theory]
    [MemberData(nameof(ForeignCursors))]
    public async Task Cursor_outside_the_list_is_rejected(string path, CursorKind kind, Guid code) =>
        Assert.Equal(["cursor"], await InvalidFieldsAsync($"{path}?cursor={PageCursor.Encode(kind, code)}"));

    public static TheoryData<string> AllPaths
    {
        get
        {
            var paths = ListPaths;
            paths.Add($"/v1/artists/{SampleCatalog.Band}");
            paths.Add($"/v1/albums/{SampleCatalog.DoubleAlbum}");
            paths.Add($"/v1/tracks/{SampleCatalog.Opening}");
            paths.Add("/v1/tracks?limit=0");

            return paths;
        }
    }

    // También con una consulta no válida: sin sesión no se revela la validación.
    [Theory]
    [MemberData(nameof(AllPaths))]
    public async Task Catalog_requires_a_session(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new("Bearer", "no-es-un-token");

        using var response = await _api.Client.SendAsync(request);

        await Problem.AssertAsync(response, HttpStatusCode.Unauthorized);
        Assert.Null(response.Headers.ETag);
    }

    [Fact]
    public async Task Single_page_has_no_next_cursor()
    {
        var page = await _api.GetJsonAsync("/v1/tracks");

        Assert.Equal(JsonValueKind.Null, page.GetProperty("nextCursor").ValueKind);
    }
}
