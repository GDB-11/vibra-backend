using BindSharp;
using Dapper;
using Npgsql;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Application.Repositories;
using Vibra.Modules.Identity.Infrastructure.Sql;
using Vibra.Shared.Infrastructure;

namespace Vibra.Modules.Identity.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository : BaseDatabaseService, IRefreshTokenRepository
{
    public RefreshTokenRepository(NpgsqlDataSource dataSource)
        : base(dataSource)
    {
    }

    public Task<Result<Unit, RepositoryError>> CreateAsync(
        NewRefreshToken token, CancellationToken cancellationToken) =>
        TryDbAsync(
            async () =>
            {
                await ExecuteAsync(RefreshTokenRepositorySql.Insert, token, cancellationToken);

                return Unit.Value;
            },
            exception => (RepositoryError)new CreateRefreshTokenFailed(exception),
            cancellationToken);

    public Task<Result<RotationOutcome, RepositoryError>> RotateAsync(
        string tokenHash,
        string replacementHash,
        DateTimeOffset replacementExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        TryTransactionAsync<RotationOutcome, RepositoryError>(
            async (connection, transaction) =>
            {
                CommandDefinition In(string sql, object parameters) =>
                    Command(sql, parameters, cancellationToken, transaction);

                var family = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    In(RefreshTokenRepositorySql.FindFamily, new { TokenHash = tokenHash }));

                if (family is null)
                    return RotationOutcome.Rejected;

                // Serializa todo lo que toca la familia. Bloquear solo la fila no basta: al revocar
                // se escaparía el token que otra transacción esté creando en este momento.
                await connection.ExecuteAsync(In(RefreshTokenRepositorySql.LockFamily, new { TokenFamily = family }));

                // Se lee después del bloqueo para ver lo que confirmó la transacción anterior.
                var current = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
                    In(RefreshTokenRepositorySql.FindByHash, new { TokenHash = tokenHash }));

                if (current is null)
                    return RotationOutcome.Rejected;

                if (current.RevokedAt is not null)
                {
                    await connection.ExecuteAsync(
                        In(RefreshTokenRepositorySql.RevokeFamily, new { TokenFamily = family, Now = now }));

                    return RotationOutcome.Rejected;
                }

                if (new DateTimeOffset(current.ExpiresAt) <= now)
                    return RotationOutcome.Rejected;

                var replacementId = await connection.ExecuteScalarAsync<long>(In(
                    RefreshTokenRepositorySql.Insert,
                    new NewRefreshToken(current.UserId, replacementHash, family.Value, replacementExpiresAt)));

                await connection.ExecuteAsync(In(
                    RefreshTokenRepositorySql.Replace,
                    new { current.RefreshTokenId, ReplacementId = replacementId, Now = now }));

                return new RotationOutcome(current.UserCode);
            },
            exception => new RotateRefreshTokenFailed(exception),
            cancellationToken);
}
