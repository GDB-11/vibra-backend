using BindSharp;
using Dapper;
using Npgsql;
using Vibra.Shared.Domain;
using Vibra.Shared.Infrastructure;

namespace Vibra.Shared.Tests.Data;

public abstract record ItemError(string Message, Exception? Cause = null) : IModuleError;

public sealed record DuplicateItemError(string Constraint) : ItemError("El elemento ya existe.");

public sealed record MissingOwnerError(string Constraint) : ItemError("El propietario no existe.");

public sealed record ItemRejectedError() : ItemError("El elemento fue rechazado.");

public sealed record ItemDataError(Exception Exception) : ItemError("Fallo al acceder a los elementos.", Exception);

public sealed record ItemRow(int ItemId, string DisplayName);

/// <summary>Repositorio de ejemplo que usa la base como lo hará un módulo.</summary>
public sealed class ProbeRepository(NpgsqlDataSource dataSource) : BaseDatabaseService(dataSource)
{
    private const string Insert = """INSERT INTO "Item" ("ItemId", "OwnerId", "DisplayName") VALUES (@Id, @OwnerId, @Name)""";

    public Task<Result<int, ItemError>> AddAsync(int id, int ownerId, CancellationToken cancellationToken) =>
        TryDbAsync(
            () => ExecuteAsync(Insert, new { Id = id, OwnerId = ownerId, Name = "item" }, cancellationToken),
            Translate,
            cancellationToken);

    public Task<Result<ItemRow?, ItemError>> FindAsync(int id, CancellationToken cancellationToken) =>
        TryDbAsync(
            () => QuerySingleOrDefaultAsync<ItemRow>(
                """SELECT "ItemId", "DisplayName" FROM "Item" WHERE "ItemId" = @Id""", new { Id = id }, cancellationToken),
            Translate,
            cancellationToken);

    public Task<Result<IReadOnlyList<ItemRow>, ItemError>> ListAsync(CancellationToken cancellationToken) =>
        TryDbAsync(
            () => QueryAsync<ItemRow>("""SELECT "ItemId", "DisplayName" FROM "Item" ORDER BY "ItemId" """, null, cancellationToken),
            Translate,
            cancellationToken);

    public Task<Result<long, ItemError>> CountAsync(CancellationToken cancellationToken) =>
        TryDbAsync(
            () => ExecuteScalarAsync<long>("""SELECT count(*) FROM "Item" """, null, cancellationToken),
            Translate,
            cancellationToken);

    public Task<Result<int, ItemError>> SleepAsync(CancellationToken cancellationToken) =>
        TryDbAsync(
            () => ExecuteAsync("SELECT pg_sleep(30)", null, cancellationToken),
            Translate,
            cancellationToken);

    /// <summary>Inserta una fila y luego ejecuta <paramref name="then"/> dentro de la misma transacción.</summary>
    public Task<Result<int, ItemError>> AddInTransactionAsync(
        int id, Func<Result<int, ItemError>> then, CancellationToken cancellationToken) =>
        TryTransactionAsync(
            async (connection, transaction) =>
            {
                await connection.ExecuteAsync(Command(
                    Insert, new { Id = id, OwnerId = 1, Name = "tx" }, cancellationToken, transaction));

                return then();
            },
            Translate,
            cancellationToken);

    private static ItemError Translate(Exception exception)
    {
        if (IsUniqueViolation(exception, out var unique))
            return new DuplicateItemError(unique);

        return IsForeignKeyViolation(exception, out var foreignKey)
            ? new MissingOwnerError(foreignKey)
            : new ItemDataError(exception);
    }
}
