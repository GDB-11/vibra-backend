namespace Vibra.Modules.Catalog.Infrastructure.Sql;

internal static class TrackRepositorySql
{
    private const string Select =
        """
        SELECT t."TrackCode", t."Title", al."AlbumCode", al."Title" AS "AlbumTitle", al."CoverKey",
               t."DiscNumber", t."TrackNumber", t."DurationMs", t."PriceCents", t."Currency", t."StoreProductId"
        FROM catalog."Track" t
        JOIN catalog."Album" al ON al."AlbumId" = t."AlbumId"
        WHERE t."Status" = 'Ready'

        """;

    public const string CursorExists =
        """SELECT EXISTS (SELECT 1 FROM catalog."Track" WHERE "TrackCode" = @After)""";

    public const string AlbumCursorExists =
        """
        SELECT EXISTS (SELECT 1 FROM catalog."Track" t
                       JOIN catalog."Album" al ON al."AlbumId" = t."AlbumId"
                       WHERE t."TrackCode" = @After AND al."AlbumCode" = @AlbumCode)
        """;

    public const string List = Select +
        """
          AND (@After::uuid IS NULL OR (t."Title", t."TrackCode") >
               (SELECT c."Title", c."TrackCode" FROM catalog."Track" c WHERE c."TrackCode" = @After))
        ORDER BY t."Title", t."TrackCode"
        LIMIT @Take
        """;

    // (disco, número) es único dentro de un álbum.
    public const string ListByAlbum = Select +
        """
          AND al."AlbumCode" = @AlbumCode
          AND (@After::uuid IS NULL OR (t."DiscNumber", t."TrackNumber") >
               (SELECT c."DiscNumber", c."TrackNumber" FROM catalog."Track" c WHERE c."TrackCode" = @After))
        ORDER BY t."DiscNumber", t."TrackNumber"
        LIMIT @Take
        """;

    public const string Find = Select + """  AND t."TrackCode" = @TrackCode""";

    public const string Credits =
        """
        SELECT t."TrackCode", ar."ArtistCode", ar."Name"
        FROM catalog."TrackArtist" ta
        JOIN catalog."Track" t ON t."TrackId" = ta."TrackId"
        JOIN catalog."Artist" ar ON ar."ArtistId" = ta."ArtistId"
        WHERE t."TrackCode" = ANY(@TrackCodes)
        ORDER BY ta."Position", ta."TrackArtistId"
        """;
}
