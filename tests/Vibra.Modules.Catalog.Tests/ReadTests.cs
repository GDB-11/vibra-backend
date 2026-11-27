using System.Net;
using System.Text.Json;

namespace Vibra.Modules.Catalog.Tests;

public sealed class ReadTests : IClassFixture<SampleCatalogFixture>
{
    private readonly CatalogApi _api;

    public ReadTests(SampleCatalogFixture fixture)
    {
        _api = fixture.Api;
    }

    private static List<string> Texts(IEnumerable<JsonElement> items, string property) =>
        [.. items.Select(item => item.GetProperty(property).GetString()!)];

    private static List<string> Names(JsonElement element) =>
        [.. element.EnumerateObject().Select(property => property.Name)];

    [Fact]
    public async Task Artists_are_listed_by_name_without_the_invisible_ones() =>
        Assert.Equal(["Banda", "Invitada"], Texts(await _api.WalkAsync("/v1/artists", 50), "name"));

    [Fact]
    public async Task Artist_exposes_only_its_public_fields()
    {
        var artist = await _api.GetJsonAsync($"/v1/artists/{SampleCatalog.Band}");

        Assert.Equal(["id", "name"], Names(artist));
        Assert.Equal(SampleCatalog.Band, artist.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Album_has_price_date_artist_and_ready_track_count()
    {
        var album = await _api.GetJsonAsync($"/v1/albums/{SampleCatalog.DoubleAlbum}");

        Assert.Equal(
            ["id", "title", "artist", "releaseDate", "trackCount", "priceCents", "currency", "storeProductId", "cover"],
            Names(album));
        Assert.Equal("2022-03-01", album.GetProperty("releaseDate").GetString());
        Assert.Equal(SampleCatalog.Band, album.GetProperty("artist").GetProperty("id").GetGuid());
        Assert.Equal(3, album.GetProperty("trackCount").GetInt32());
        Assert.Equal(2490, album.GetProperty("priceCents").GetInt32());
        Assert.Equal("PEN", album.GetProperty("currency").GetString());
        Assert.Equal("vibra.album.doble", album.GetProperty("storeProductId").GetString());
        // Sin Images:BaseUrl no hay portada.
        Assert.Equal(JsonValueKind.Null, album.GetProperty("cover").ValueKind);
    }

    [Fact]
    public async Task Track_has_price_album_and_credits_in_order()
    {
        var track = await _api.GetJsonAsync($"/v1/tracks/{SampleCatalog.Opening}");

        Assert.Equal(
            ["id", "title", "album", "artists", "discNumber", "trackNumber", "durationMs", "priceCents",
             "currency", "storeProductId"],
            Names(track));
        Assert.Equal(SampleCatalog.DoubleAlbum, track.GetProperty("album").GetProperty("id").GetGuid());
        Assert.Equal(["Invitada", "Banda"], Texts(track.GetProperty("artists").EnumerateArray(), "name"));
        Assert.Equal(200000, track.GetProperty("durationMs").GetInt32());
        Assert.Equal(340, track.GetProperty("priceCents").GetInt32());
        Assert.Equal("PEN", track.GetProperty("currency").GetString());
        Assert.Equal("vibra.track.apertura", track.GetProperty("storeProductId").GetString());
    }

    [Fact]
    public async Task Store_product_id_may_be_null()
    {
        var album = await _api.GetJsonAsync($"/v1/albums/{SampleCatalog.FirstEcho}");

        Assert.Equal(JsonValueKind.Null, album.GetProperty("storeProductId").ValueKind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(50)]
    public async Task Tracks_are_walked_by_title_without_gaps_or_repeats(int limit)
    {
        var tracks = await _api.WalkAsync("/v1/tracks", limit);

        // "Señal" está dos veces; "Borrador" y "Rota" no están listas.
        Assert.Equal(["Apertura", "Brisa", "Coda", "Señal", "Señal"], Texts(tracks, "title"));
        Assert.Equal(5, Texts(tracks, "id").Distinct().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Albums_are_walked_by_title_without_gaps_or_repeats(int limit)
    {
        var albums = await _api.WalkAsync("/v1/albums", limit);

        Assert.Equal(["Doble", "Eco", "Eco"], Texts(albums, "title"));
        Assert.Equal(3, Texts(albums, "id").Distinct().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Artists_are_walked_page_by_page(int limit) =>
        Assert.Equal(["Banda", "Invitada"], Texts(await _api.WalkAsync("/v1/artists", limit), "name"));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Artist_albums_are_ordered_by_release_date(int limit)
    {
        var albums = await _api.WalkAsync($"/v1/artists/{SampleCatalog.Band}/albums", limit);

        Assert.Equal(["2020-01-15", "2021-06-30", "2022-03-01"], Texts(albums, "releaseDate"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Album_tracks_are_ordered_by_disc_and_number(int limit)
    {
        var tracks = await _api.WalkAsync($"/v1/albums/{SampleCatalog.DoubleAlbum}/tracks", limit);

        Assert.Equal(["Apertura", "Brisa", "Coda"], Texts(tracks, "title"));
    }

    [Fact]
    public async Task Credited_artist_without_albums_has_an_empty_album_list()
    {
        var page = await _api.GetJsonAsync($"/v1/artists/{SampleCatalog.Guest}/albums");

        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, page.GetProperty("nextCursor").ValueKind);
    }

    [Theory]
    [InlineData("artists", "artist-not-found")]
    [InlineData("albums", "album-not-found")]
    [InlineData("tracks", "track-not-found")]
    public async Task Unknown_code_is_not_found(string resource, string type)
    {
        using var response = await _api.Client.GetAsync($"/v1/{resource}/{Guid.NewGuid()}");

        await Problem.AssertAsync(response, HttpStatusCode.NotFound, type);
    }

    public static TheoryData<string, string> HiddenPaths => new()
    {
        { $"/v1/artists/{SampleCatalog.Ghost}", "artist-not-found" },
        { $"/v1/artists/{SampleCatalog.Ghost}/albums", "artist-not-found" },
        { $"/v1/albums/{SampleCatalog.HiddenAlbum}", "album-not-found" },
        { $"/v1/albums/{SampleCatalog.HiddenAlbum}/tracks", "album-not-found" },
        { $"/v1/tracks/{SampleCatalog.PendingTrack}", "track-not-found" },
        { $"/v1/tracks/{SampleCatalog.FailedTrack}", "track-not-found" },
        { $"/v1/artists/{Guid.Empty}/albums", "artist-not-found" },
        { $"/v1/albums/{Guid.Empty}/tracks", "album-not-found" },
    };

    [Theory]
    [MemberData(nameof(HiddenPaths))]
    public async Task Hidden_or_missing_items_are_not_found(string path, string type)
    {
        using var response = await _api.Client.GetAsync(path);

        await Problem.AssertAsync(response, HttpStatusCode.NotFound, type);
    }

    [Theory]
    [InlineData("/v1/artists/1")]
    [InlineData("/v1/albums/no-es-un-uuid")]
    [InlineData("/v1/tracks/1")]
    [InlineData("/v1/albums/1/tracks")]
    public async Task Id_that_is_not_a_uuid_is_not_found(string path)
    {
        using var response = await _api.Client.GetAsync(path);

        await Problem.AssertAsync(response, HttpStatusCode.NotFound);
    }
}
