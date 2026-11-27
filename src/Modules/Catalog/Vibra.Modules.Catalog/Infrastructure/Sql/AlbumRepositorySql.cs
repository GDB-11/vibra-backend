namespace Vibra.Modules.Catalog.Infrastructure.Sql;

internal static class AlbumRepositorySql
{
    // Un álbum sin pistas Ready no es visible.
    private const string Select =
        """
        SELECT al."AlbumCode", al."Title", ar."ArtistCode", ar."Name" AS "ArtistName", al."ReleaseDate",
               al."CoverKey", ready."TrackCount", al."PriceCents", al."Currency", al."StoreProductId"
        FROM catalog."Album" al
        JOIN catalog."Artist" ar ON ar."ArtistId" = al."ArtistId"
        CROSS JOIN LATERAL (SELECT count(*)::integer AS "TrackCount" FROM catalog."Track" t
                            WHERE t."AlbumId" = al."AlbumId" AND t."Status" = 'Ready') ready
        WHERE ready."TrackCount" > 0

        """;

    public const string CursorExists =
        """SELECT EXISTS (SELECT 1 FROM catalog."Album" WHERE "AlbumCode" = @After)""";

    public const string ArtistCursorExists =
        """
        SELECT EXISTS (SELECT 1 FROM catalog."Album" al
                       JOIN catalog."Artist" ar ON ar."ArtistId" = al."ArtistId"
                       WHERE al."AlbumCode" = @After AND ar."ArtistCode" = @ArtistCode)
        """;

    public const string List = Select +
        """
          AND (@After::uuid IS NULL OR (al."Title", al."AlbumCode") >
               (SELECT c."Title", c."AlbumCode" FROM catalog."Album" c WHERE c."AlbumCode" = @After))
        ORDER BY al."Title", al."AlbumCode"
        LIMIT @Take
        """;

    public const string ListByArtist = Select +
        """
          AND ar."ArtistCode" = @ArtistCode
          AND (@After::uuid IS NULL OR (al."ReleaseDate", al."AlbumCode") >
               (SELECT c."ReleaseDate", c."AlbumCode" FROM catalog."Album" c WHERE c."AlbumCode" = @After))
        ORDER BY al."ReleaseDate", al."AlbumCode"
        LIMIT @Take
        """;

    public const string Find = Select + """  AND al."AlbumCode" = @AlbumCode""";
}
