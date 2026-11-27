using Vibra.Modules.Identity.Domain;

namespace Vibra.Modules.Identity.Infrastructure.Repositories;

internal sealed record UserKeysRow(long UserId, Guid UserCode);

// Npgsql entrega timestamptz como DateTime en UTC.
internal sealed record UserRow(
    long UserId,
    Guid UserCode,
    string Email,
    string NormalizedEmail,
    string PasswordHash,
    string DisplayName,
    string SecurityStamp,
    string ConcurrencyStamp,
    DateTime? LockoutEnd,
    int AccessFailedCount)
{
    public User ToUser() =>
        new()
        {
            UserId = UserId,
            UserCode = UserCode,
            Email = Email,
            NormalizedEmail = NormalizedEmail,
            PasswordHash = PasswordHash,
            DisplayName = DisplayName,
            SecurityStamp = SecurityStamp,
            ConcurrencyStamp = ConcurrencyStamp,
            LockoutEnd = LockoutEnd is { } end ? new DateTimeOffset(end) : null,
            AccessFailedCount = AccessFailedCount,
        };
}

internal sealed record RefreshTokenRow(
    long RefreshTokenId, long UserId, Guid UserCode, DateTime ExpiresAt, DateTime? RevokedAt);
