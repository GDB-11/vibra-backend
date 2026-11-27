namespace Vibra.Modules.Catalog.Tests;

/// <summary>
/// Datos de prueba. "Banda" tiene "Doble" (dos discos, 2022) y dos álbumes llamados "Eco" (2020 y 2021).
/// "Invitada" solo aparece en un crédito. "Fantasma" no tiene nada visible.
/// </summary>
internal static class SampleCatalog
{
    public static readonly Guid Band = new("a0000000-0000-4000-8000-000000000001");
    public static readonly Guid Guest = new("a0000000-0000-4000-8000-000000000002");
    public static readonly Guid Ghost = new("a0000000-0000-4000-8000-000000000003");
    public static readonly Guid DoubleAlbum = new("b0000000-0000-4000-8000-000000000001");
    public static readonly Guid FirstEcho = new("b0000000-0000-4000-8000-000000000002");
    public static readonly Guid SecondEcho = new("b0000000-0000-4000-8000-000000000003");
    public static readonly Guid HiddenAlbum = new("b0000000-0000-4000-8000-000000000004");
    public static readonly Guid CoverKey = new("c0000000-0000-4000-8000-000000000001");
    public static readonly Guid Opening = new("d0000000-0000-4000-8000-000000000001");
    public static readonly Guid PendingTrack = new("d0000000-0000-4000-8000-000000000004");
    public static readonly Guid FailedTrack = new("d0000000-0000-4000-8000-000000000007");

    public const string Sql =
        """
        INSERT INTO catalog."Artist" ("ArtistCode", "Name") VALUES
            ('a0000000-0000-4000-8000-000000000001', 'Banda'),
            ('a0000000-0000-4000-8000-000000000002', 'Invitada'),
            ('a0000000-0000-4000-8000-000000000003', 'Fantasma');

        INSERT INTO catalog."Album"
            ("AlbumCode", "ArtistId", "Title", "ReleaseDate", "CoverKey", "PriceCents", "Currency", "StoreProductId")
        SELECT v.code::uuid, a."ArtistId", v.title, v.released::date, v.cover::uuid, v.price, 'PEN', v.product
        FROM (VALUES
            ('b0000000-0000-4000-8000-000000000001', 'a0000000-0000-4000-8000-000000000001', 'Doble', '2022-03-01',
             'c0000000-0000-4000-8000-000000000001', 2490, 'vibra.album.doble'),
            ('b0000000-0000-4000-8000-000000000002', 'a0000000-0000-4000-8000-000000000001', 'Eco', '2020-01-15',
             NULL, 1000, NULL),
            ('b0000000-0000-4000-8000-000000000003', 'a0000000-0000-4000-8000-000000000001', 'Eco', '2021-06-30',
             NULL, 1100, NULL),
            ('b0000000-0000-4000-8000-000000000004', 'a0000000-0000-4000-8000-000000000003', 'Oculto', '2023-01-01',
             NULL, 900, NULL)
        ) AS v(code, artist, title, released, cover, price, product)
        JOIN catalog."Artist" a ON a."ArtistCode" = v.artist::uuid;

        INSERT INTO catalog."Track"
            ("TrackCode", "AlbumId", "Title", "DiscNumber", "TrackNumber", "DurationMs", "Status",
             "PriceCents", "Currency", "StoreProductId")
        SELECT v.code::uuid, al."AlbumId", v.title, v.disc, v.number, 200000, v.status, 340, 'PEN', v.product
        FROM (VALUES
            ('d0000000-0000-4000-8000-000000000001', 'b0000000-0000-4000-8000-000000000001', 'Apertura', 1, 1,
             'Ready', 'vibra.track.apertura'),
            -- El disco 2 se inserta antes que la pista 2 del disco 1 para comprobar el orden.
            ('d0000000-0000-4000-8000-000000000002', 'b0000000-0000-4000-8000-000000000001', 'Coda', 2, 1,
             'Ready', NULL),
            ('d0000000-0000-4000-8000-000000000003', 'b0000000-0000-4000-8000-000000000001', 'Brisa', 1, 2,
             'Ready', NULL),
            ('d0000000-0000-4000-8000-000000000004', 'b0000000-0000-4000-8000-000000000001', 'Borrador', 1, 3,
             'Pending', NULL),
            ('d0000000-0000-4000-8000-000000000005', 'b0000000-0000-4000-8000-000000000002', 'Señal', 1, 1,
             'Ready', NULL),
            ('d0000000-0000-4000-8000-000000000006', 'b0000000-0000-4000-8000-000000000003', 'Señal', 1, 1,
             'Ready', NULL),
            ('d0000000-0000-4000-8000-000000000007', 'b0000000-0000-4000-8000-000000000004', 'Rota', 1, 1,
             'Failed', NULL)
        ) AS v(code, album, title, disc, number, status, product)
        JOIN catalog."Album" al ON al."AlbumCode" = v.album::uuid;

        -- En "Apertura", la invitada va primero aunque su crédito se inserte después.
        INSERT INTO catalog."TrackArtist" ("TrackId", "ArtistId", "Position")
        SELECT t."TrackId", a."ArtistId", v.position
        FROM (VALUES
            ('d0000000-0000-4000-8000-000000000001', 'a0000000-0000-4000-8000-000000000001', 2),
            ('d0000000-0000-4000-8000-000000000001', 'a0000000-0000-4000-8000-000000000002', 1),
            ('d0000000-0000-4000-8000-000000000003', 'a0000000-0000-4000-8000-000000000001', 1),
            ('d0000000-0000-4000-8000-000000000007', 'a0000000-0000-4000-8000-000000000003', 1)
        ) AS v(track, artist, position)
        JOIN catalog."Track" t ON t."TrackCode" = v.track::uuid
        JOIN catalog."Artist" a ON a."ArtistCode" = v.artist::uuid;
        """;
}

/// <summary>Una API con los datos de prueba, compartida por las pruebas de solo lectura.</summary>
public sealed class SampleCatalogFixture : IAsyncLifetime
{
    private readonly Vibra.Migrations.Tests.PostgresFixture _postgres = new();

    public CatalogApi Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        Api = await CatalogApi.StartAsync(_postgres);
        await Api.ExecuteAsync(SampleCatalog.Sql);
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
