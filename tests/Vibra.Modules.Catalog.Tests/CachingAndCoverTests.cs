using System.Net;
using System.Text.Json;
using Vibra.Migrations.Tests;

namespace Vibra.Modules.Catalog.Tests;

public sealed class CachingAndCoverTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public CachingAndCoverTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private async Task<CatalogApi> StartAsync(IReadOnlyDictionary<string, string>? settings = null)
    {
        var api = await CatalogApi.StartAsync(_postgres, settings);
        await api.ExecuteAsync(SampleCatalog.Sql);

        return api;
    }

    private static async Task<HttpResponseMessage> GetIfNoneMatchAsync(CatalogApi api, string path, string value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("If-None-Match", value);

        return await api.Client.SendAsync(request);
    }

    [Fact]
    public async Task Every_read_has_an_etag_and_answers_304_when_it_matches()
    {
        await using var api = await StartAsync();
        string[] paths =
        [
            "/v1/artists", $"/v1/artists/{SampleCatalog.Band}", $"/v1/artists/{SampleCatalog.Band}/albums",
            "/v1/albums", $"/v1/albums/{SampleCatalog.DoubleAlbum}",
            $"/v1/albums/{SampleCatalog.DoubleAlbum}/tracks", "/v1/tracks", $"/v1/tracks/{SampleCatalog.Opening}",
        ];

        foreach (var path in paths)
        {
            using var first = await api.Client.GetAsync(path);
            var etag = first.Headers.ETag;

            Assert.True(first.StatusCode == HttpStatusCode.OK && etag is { IsWeak: false }, path);
            Assert.Equal("application/json", first.Content.Headers.ContentType?.MediaType);
            Assert.True(first.Headers.CacheControl is { Private: true, NoCache: true }, path);

            // Coincidencia exacta, débil, dentro de una lista y comodín.
            foreach (var value in new[] { etag.Tag, "W/" + etag.Tag, "\"otro\", " + etag.Tag, "*" })
            {
                using var cached = await GetIfNoneMatchAsync(api, path, value);

                Assert.True(cached.StatusCode == HttpStatusCode.NotModified, $"{path} {value}: {cached.StatusCode}");
                Assert.Equal(etag.Tag, cached.Headers.ETag?.Tag);
                Assert.Empty(await cached.Content.ReadAsByteArrayAsync());
            }

            using var stale = await GetIfNoneMatchAsync(api, path, "\"otro\"");

            Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
        }
    }

    [Fact]
    public async Task Etag_changes_when_the_data_changes()
    {
        await using var api = await StartAsync();
        var path = $"/v1/tracks/{SampleCatalog.Opening}";
        using var before = await api.Client.GetAsync(path);

        await api.ExecuteAsync(
            """UPDATE catalog."Track" SET "PriceCents" = 500 WHERE "TrackCode" = @Code""",
            new { Code = SampleCatalog.Opening });

        using var after = await GetIfNoneMatchAsync(api, path, before.Headers.ETag!.Tag);

        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.NotEqual(before.Headers.ETag, after.Headers.ETag);
    }

    [Fact]
    public async Task Errors_have_no_etag()
    {
        await using var api = await StartAsync();

        using var response = await GetIfNoneMatchAsync(api, $"/v1/tracks/{Guid.NewGuid()}", "*");

        await Problem.AssertAsync(response, HttpStatusCode.NotFound, "track-not-found");
        Assert.Null(response.Headers.ETag);
    }

    [Fact]
    public async Task Empty_catalog_returns_empty_pages()
    {
        await using var api = await CatalogApi.StartAsync(_postgres);

        foreach (var path in new[] { "/v1/artists", "/v1/albums", "/v1/tracks" })
            Assert.Empty(await api.WalkAsync(path, 20));
    }

    [Fact]
    public async Task Default_page_has_twenty_items()
    {
        await using var api = await CatalogApi.StartAsync(_postgres);
        await api.ExecuteAsync(SampleCatalog.Sql);
        await api.ExecuteAsync(
            """
            INSERT INTO catalog."Track" ("AlbumId", "Title", "DiscNumber", "TrackNumber", "DurationMs", "Status",
                                         "PriceCents", "Currency")
            SELECT "AlbumId", 'Extra ' || n, 3, n, 1, 'Ready', 1, 'PEN'
            FROM catalog."Album", generate_series(1, 20) AS n
            WHERE "AlbumCode" = @Code
            """,
            new { Code = SampleCatalog.DoubleAlbum });

        var page = await api.GetJsonAsync("/v1/tracks");

        Assert.Equal(20, page.GetProperty("items").GetArrayLength());
        Assert.NotNull(page.GetProperty("nextCursor").GetString());
    }

    [Fact]
    public async Task Database_failure_does_not_leak_details()
    {
        await using var api = await StartAsync();
        await api.ExecuteAsync("""ALTER TABLE catalog."TrackArtist" RENAME TO "Rota" """);

        using var response = await api.Client.GetAsync("/v1/tracks");
        var problem = await Problem.AssertAsync(response, HttpStatusCode.InternalServerError, "unexpected");

        Assert.DoesNotContain("TrackArtist", problem.ToString());
    }

    [Theory]
    [InlineData("https://cdn.example.com")]
    [InlineData("https://cdn.example.com/")]
    public async Task Cover_urls_are_built_from_the_configured_cdn(string baseUrl)
    {
        await using var api = await StartAsync(new Dictionary<string, string> { ["Images:BaseUrl"] = baseUrl });

        var album = await api.GetJsonAsync($"/v1/albums/{SampleCatalog.DoubleAlbum}");
        var track = await api.GetJsonAsync($"/v1/tracks/{SampleCatalog.Opening}");
        var withoutCover = await api.GetJsonAsync($"/v1/albums/{SampleCatalog.FirstEcho}");

        string[] expected =
        [
            .. new[] { 300, 600, 800 }.Select(width =>
                $"{width} https://cdn.example.com/covers/{SampleCatalog.CoverKey}/{width}.jpg"),
        ];

        Assert.Equal(expected, Covers(album));
        Assert.Equal(expected, Covers(track.GetProperty("album")));
        Assert.Equal(JsonValueKind.Null, withoutCover.GetProperty("cover").ValueKind);
    }

    [Fact]
    public async Task Configured_widths_replace_the_default_ones()
    {
        await using var api = await StartAsync(new Dictionary<string, string>
        {
            ["Images:BaseUrl"] = "https://cdn.example.com",
            ["Images:CoverWidths:0"] = "128",
        });

        var album = await api.GetJsonAsync($"/v1/albums/{SampleCatalog.DoubleAlbum}");

        Assert.Equal([$"128 https://cdn.example.com/covers/{SampleCatalog.CoverKey}/128.jpg"], Covers(album));
    }

    [Theory]
    [InlineData("Images:BaseUrl", "http://cdn.example.com")]
    [InlineData("Images:BaseUrl", "/covers")]
    [InlineData("Images:BaseUrl", "https://cdn.example.com?x=1")]
    [InlineData("Images:CoverWidths:0", "0")]
    public async Task Host_does_not_start_with_invalid_image_settings(string key, string value) =>
        await Assert.ThrowsAnyAsync<Exception>(
            () => CatalogApi.StartAsync(_postgres, new Dictionary<string, string> { [key] = value }));

    private static string[] Covers(JsonElement owner) =>
    [
        .. owner.GetProperty("cover").EnumerateArray().Select(image =>
            $"{image.GetProperty("width").GetInt32()} {image.GetProperty("url").GetString()}"),
    ];
}
