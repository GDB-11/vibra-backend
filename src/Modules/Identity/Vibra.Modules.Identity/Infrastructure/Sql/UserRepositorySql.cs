namespace Vibra.Modules.Identity.Infrastructure.Sql;

internal static class UserRepositorySql
{
    public const string Insert =
        """
        INSERT INTO identity."User"
            ("Email", "NormalizedEmail", "PasswordHash", "DisplayName", "SecurityStamp",
             "ConcurrencyStamp", "LockoutEnd", "AccessFailedCount")
        VALUES
            (@Email, @NormalizedEmail, @PasswordHash, @DisplayName, @SecurityStamp,
             @ConcurrencyStamp, @LockoutEnd, @AccessFailedCount)
        RETURNING "UserId", "UserCode"
        """;

    private const string Select =
        """
        SELECT "UserId", "UserCode", "Email", "NormalizedEmail", "PasswordHash", "DisplayName",
               "SecurityStamp", "ConcurrencyStamp", "LockoutEnd", "AccessFailedCount"
        FROM identity."User"

        """;

    public const string FindById = Select + """WHERE "UserId" = @UserId""";

    public const string FindByNormalizedEmail = Select + """WHERE "NormalizedEmail" = @NormalizedEmail""";

    public const string Update =
        """
        UPDATE identity."User"
        SET "Email" = @Email, "NormalizedEmail" = @NormalizedEmail, "PasswordHash" = @PasswordHash,
            "DisplayName" = @DisplayName, "SecurityStamp" = @SecurityStamp,
            "ConcurrencyStamp" = @NewConcurrencyStamp, "LockoutEnd" = @LockoutEnd,
            "AccessFailedCount" = @AccessFailedCount
        WHERE "UserId" = @UserId AND "ConcurrencyStamp" = @ConcurrencyStamp
        """;

    public const string Delete = """DELETE FROM identity."User" WHERE "UserId" = @UserId""";

    // Las expresiones de la derecha ven el valor anterior de la fila, bloqueada por el UPDATE.
    public const string RecordFailedAccess =
        """
        UPDATE identity."User"
        SET "AccessFailedCount" = CASE WHEN "AccessFailedCount" + 1 >= @MaxAttempts
                                       THEN 0 ELSE "AccessFailedCount" + 1 END,
            "LockoutEnd" = CASE WHEN "AccessFailedCount" + 1 >= @MaxAttempts
                                THEN @LockoutEnd ELSE "LockoutEnd" END,
            "ConcurrencyStamp" = @ConcurrencyStamp
        WHERE "UserId" = @UserId
        """;

    public const string ResetFailedAccess =
        """
        UPDATE identity."User"
        SET "AccessFailedCount" = 0, "ConcurrencyStamp" = @ConcurrencyStamp
        WHERE "UserId" = @UserId AND "AccessFailedCount" <> 0
        """;
}
