namespace Vibra.Modules.Identity.Infrastructure.Sql;

internal static class RefreshTokenRepositorySql
{
    public const string Insert =
        """
        INSERT INTO identity."RefreshToken" ("UserId", "TokenHash", "TokenFamily", "ExpiresAt")
        VALUES (@UserId, @TokenHash, @TokenFamily, @ExpiresAt)
        RETURNING "RefreshTokenId"
        """;

    public const string FindFamily =
        """SELECT "TokenFamily" FROM identity."RefreshToken" WHERE "TokenHash" = @TokenHash""";

    // Se libera al terminar la transacción.
    public const string LockFamily =
        "SELECT pg_advisory_xact_lock(hashtextextended('identity.RefreshToken:' || @TokenFamily::text, 0))";

    public const string FindByHash =
        """
        SELECT t."RefreshTokenId", t."UserId", u."UserCode", t."ExpiresAt", t."RevokedAt"
        FROM identity."RefreshToken" t
        JOIN identity."User" u ON u."UserId" = t."UserId"
        WHERE t."TokenHash" = @TokenHash
        """;

    public const string RevokeFamily =
        """
        UPDATE identity."RefreshToken" SET "RevokedAt" = @Now
        WHERE "TokenFamily" = @TokenFamily AND "RevokedAt" IS NULL
        """;

    public const string Replace =
        """
        UPDATE identity."RefreshToken"
        SET "RevokedAt" = @Now, "ReplacedByRefreshTokenId" = @ReplacementId
        WHERE "RefreshTokenId" = @RefreshTokenId
        """;
}
