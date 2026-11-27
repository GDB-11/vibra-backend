namespace Vibra.Modules.Catalog.Infrastructure.Sql;

internal static class ArtistRepositorySql
{
    // Visible: tiene un álbum con alguna pista Ready, o un crédito en una pista Ready.
    private const string Select =
        """
        SELECT a."ArtistCode", a."Name"
        FROM catalog."Artist" a
        WHERE (EXISTS (SELECT 1 FROM catalog."Album" al
                       JOIN catalog."Track" t ON t."AlbumId" = al."AlbumId"
                       WHERE al."ArtistId" = a."ArtistId" AND t."Status" = 'Ready')
               OR EXISTS (SELECT 1 FROM catalog."TrackArtist" ta
                          JOIN catalog."Track" t ON t."TrackId" = ta."TrackId"
                          WHERE ta."ArtistId" = a."ArtistId" AND t."Status" = 'Ready'))

        """;

    public const string CursorExists =
        """SELECT EXISTS (SELECT 1 FROM catalog."Artist" WHERE "ArtistCode" = @After)""";

    public const string List = Select +
        """
          AND (@After::uuid IS NULL OR (a."Name", a."ArtistCode") >
               (SELECT c."Name", c."ArtistCode" FROM catalog."Artist" c WHERE c."ArtistCode" = @After))
        ORDER BY a."Name", a."ArtistCode"
        LIMIT @Take
        """;

    public const string Find = Select + """  AND a."ArtistCode" = @ArtistCode""";
}
